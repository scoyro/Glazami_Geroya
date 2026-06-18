mergeInto(LibraryManager.library, {
  GG_ReturnToSite: function (urlPtr) {
    var url = UTF8ToString(urlPtr);

    try {
      window.localStorage.setItem("gg_completed_aldar", "true");
    } catch (e) {
    }

    window.location.href = url;
  }
});