mergeInto(LibraryManager.library, {
    WebGL_ShowBanner: function () {
        if (window.WebAd && window.WebAd.showBanner) window.WebAd.showBanner();
    },
    WebGL_HideBanner: function () {
        if (window.WebAd && window.WebAd.hideBanner) window.WebAd.hideBanner();
    },
    WebGL_ShowRewarded: function () {
        return (window.WebAd && window.WebAd.showRewarded) ? window.WebAd.showRewarded() : false;
    }
});