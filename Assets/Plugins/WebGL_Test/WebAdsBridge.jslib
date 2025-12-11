mergeInto(LibraryManager.library, {

  WebGLAdsInit: function () {
    if (typeof adBreak === 'undefined') {
      window.adsbygoogle = window.adsbygoogle || [];
      window.adBreak = function (o) { adsbygoogle.push(o); };
    }
  },

  // 开屏
  WebGLAdsShowOpen: function () {
    adBreak({
      type: 'start', name: 'webgl-open', ad_slot: window.webglAdConfig.openAdSlot,
      beforeAd: function () { unityInstance.SendMessage('WebGLAdsManager', 'JS_BeforeAd'); },
      afterAd:  function () { unityInstance.SendMessage('WebGLAdsManager', 'JS_AfterAd'); },
      adBreakDone: function (info) {
        unityInstance.SendMessage('WebGLAdsManager', 'JS_OpenAdDone', info.error || 'ok');
      }
    });
  },

  // 横幅（用最小尺寸 start 充当）
  WebGLAdsShowBanner: function () {
    adBreak({
      type: 'start', name: 'webgl-banner',
      beforeAd: function () {},
      afterAd:  function () {},
      adBreakDone: function (info) {
        unityInstance.SendMessage('WebGLAdsManager', 'JS_BannerDone', info.error || 'ok');
      }
    });
  },

  // 激励
  WebGLAdsShowRewarded: function () {
    adBreak({
      type: 'reward', name: 'webgl-reward',
      beforeAd: function () { unityInstance.SendMessage('WebGLAdsManager', 'JS_BeforeAd'); },
      afterAd:  function () { unityInstance.SendMessage('WebGLAdsManager', 'JS_AfterAd'); },
      adBreakDone: function (info) {
        var res = (info.error ? 'fail' : 'ok') + '|' + (info.error || '');
        unityInstance.SendMessage('WebGLAdsManager', 'JS_RewardedDone', res);
      }
    });
  }

});