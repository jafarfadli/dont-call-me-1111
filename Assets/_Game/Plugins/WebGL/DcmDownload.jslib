// Browser helpers for Don't Call Me! (WebGL builds only).
mergeInto(LibraryManager.library, {
  // Saves a text file through the browser's download (the playtest log, see PlaytestLog.Export).
  DCM_DownloadText: function (fileNamePtr, textPtr) {
    var fileName = UTF8ToString(fileNamePtr);
    var text = UTF8ToString(textPtr);
    var blob = new Blob([text], { type: "application/x-ndjson" });
    var link = document.createElement("a");
    link.href = URL.createObjectURL(blob);
    link.download = fileName;
    document.body.appendChild(link);
    link.click();
    setTimeout(function () {
      URL.revokeObjectURL(link.href);
      link.remove();
    }, 1000);
  },
});
