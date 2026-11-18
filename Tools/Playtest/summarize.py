#!/usr/bin/env python3
"""Summarize Don't Call Me! playtest logs (JSON Lines from PlaytestLog) as Markdown.

Usage:
    python3 Tools/Playtest/summarize.py LOG.jsonl [MORE.jsonl ...]   # files from players
    python3 Tools/Playtest/summarize.py FOLDER                       # every *.jsonl in it
    python3 Tools/Playtest/summarize.py                              # the editor's Temp/Playtest

Each player's game writes one line per action and a "day_end" line per finished day. The report
answers the questions the notebook was built for: do players judge better by Day 3 than on Day 1,
how many false claims they catch, how many decoys they circle, how long until the first circle,
where they look first, and whether players who stamp claims also judge better.
"""
import collections
import glob
import json
import os
import statistics
import sys


def load(paths):
    lines = []
    for p in paths:
        files = sorted(glob.glob(os.path.join(p, "*.jsonl"))) if os.path.isdir(p) else [p]
        for f in files:
            with open(f, encoding="utf-8") as fh:
                for n, raw in enumerate(fh, 1):
                    raw = raw.strip()
                    if not raw:
                        continue
                    try:
                        lines.append(json.loads(raw))
                    except json.JSONDecodeError:
                        print(f"<!-- skipped a broken line: {f}:{n} -->")
    return lines


def pct(part, whole):
    return f"{100 * part / whole:.0f}%" if whole else "-"


def median(values):
    return f"{statistics.median(values):.0f}" if values else "-"


PLACES = {"ChatRead": "Chats", "BankOpened": "Bank", "ContactsOpened": "Contacts",
          "NumberChecked": "Computer (a lookup)", "DocumentViewed": "Desk drawer"}


def place(first_look):
    """Where a "first_look" was: the panel's name, or the app."""
    kind, _, target = first_look.partition(":")
    return target if kind == "PanelOpened" else PLACES.get(kind, kind)


def table(head, rows):
    out = ["| " + " | ".join(head) + " |", "|" + "|".join(" --- " for _ in head) + "|"]
    out += ["| " + " | ".join(str(c) for c in r) + " |" for r in rows]
    return "\n".join(out)


def print_table(head, rows):
    print(table(head, rows) + "\n")


def main():
    here = os.path.dirname(os.path.abspath(__file__))
    paths = sys.argv[1:] or [os.path.join(here, "..", "..", "Temp", "Playtest")]
    events = load(paths)
    ends = [e for e in events if e.get("kind") == "day_end"]
    sessions = sorted({e.get("session") for e in events})
    print("# Don't Call Me! playtest summary\n")
    print(f"{len(sessions)} sessions, {len(ends)} finished days, {len(events)} logged actions.\n")
    if not ends:
        print("No finished days yet.")
        return

    print("## Right verdicts by day\n")
    by_day = collections.defaultdict(list)
    for e in ends:
        by_day[e["day"]].append(e)
    rows = []
    for day in sorted(by_day):
        d = by_day[day]
        right = sum(e["right"] for e in d)
        rows.append([f"Day {day}", d[0].get("help", ""), len(d), right, pct(right, len(d)),
                     median([e["minutes"] for e in d])])
    print_table(["Day", "Notebook help", "Played", "Right", "Right %", "Median minutes"], rows)

    print("## Per case and truth\n")
    by_case = collections.defaultdict(list)
    for e in ends:
        by_case[(e["day"], e.get("case", ""), "scam" if e.get("scam") else "legit")].append(e)
    rows = []
    for (day, case, truth), d in sorted(by_case.items()):
        right = sum(e["right"] for e in d)
        false_claims = sum(e["false_claims"] for e in d)
        caught = sum(e["caught"] for e in d)
        rows.append([f"Day {day}", case, truth, len(d), pct(right, len(d)),
                     f"{caught}/{false_claims}" if false_claims else "-",
                     sum(e["wrong_stamps"] for e in d),
                     f"{sum(e['decoys'] for e in d) / len(d):.1f}",
                     f"{sum(e['clues'] for e in d)}/{sum(e['clues_total'] for e in d)}"])
    print_table(["Day", "Case", "Truth", "Played", "Right %", "False claims caught", "Wrong stamps",
                 "Decoys circled (avg)", "Clues found"], rows)

    print("## Looking\n")
    first = [e["first_circle_s"] for e in ends if e.get("first_circle_s", -1) >= 0]
    never = sum(1 for e in ends if e.get("circled", 0) == 0)
    print(f"Seconds from answering the call to the first circle: median {median(first)}"
          f" (nothing circled on {never} of {len(ends)} days).\n")
    looks = collections.Counter(place(e["first_look"]) for e in ends if e.get("first_look"))
    print_table(["First place looked", "Days"], [[k, v] for k, v in looks.most_common()])

    print("## Do players who stamp judge better?\n")
    groups = {"Caught at least one false claim": [], "Stamped, caught none": [], "Never stamped": []}
    for e in ends:
        if not e.get("scam"):
            continue
        key = ("Caught at least one false claim" if e["caught"] > 0
               else "Stamped, caught none" if e["stamps"] > 0 else "Never stamped")
        groups[key].append(e)
    rows = [[k, len(v), pct(sum(e["right"] for e in v), len(v))] for k, v in groups.items()]
    print_table(["On scam days", "Days", "Right %"], rows)

    print("## Asked and skipped\n")
    asks = collections.Counter(e["target"] for e in events if e.get("kind") == "ask")
    skips = sum(1 for e in events if e.get("kind") == "skip")
    pins_full = sum(1 for e in events if e.get("kind") == "pin_full")
    print(f"Lines skipped: {skips}. Pins refused (notebook full): {pins_full}.\n")
    if asks:
        print_table(["Question asked", "Times"], [[k, v] for k, v in asks.most_common()])


if __name__ == "__main__":
    main()
