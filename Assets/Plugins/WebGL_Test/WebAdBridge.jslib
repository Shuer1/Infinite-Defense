mergeInto(LibraryManager.library, {

  // 初始化 AdSense forGames 环境
  AdsInit: function () {
    if (typeof adBreak === 'undefined') {
      window.adsbygoogle = window.adsbygoogle || [];
      window.adBreak = function (o) { adsbygoogle.push(o); };
    }
  },

  // 开屏
  AdsShowOpen: function () {
    adBreak({
      type: 'start',
      name: 'webgl-open',
      beforeAd: function () {
        unityInstance.SendMessage('WebGLAdsManager', 'JS_BeforeAd');
      },
      afterAd: function () {
        unityInstance.SendMessage('WebGLAdsManager', 'JS_AfterAd');
      },
      adBreakDone: function (info) {
        var err = info.error || 'ok';
        unityInstance.SendMessage('WebGLAdsManager', 'JS_OpenAdDone', err);
      }
    });
  },

  // 横幅
  AdsShowBanner: function () {
    adBreak({
      type: 'start',
      name: 'webgl-banner',
      beforeAd: function () {},
      afterAd: function () {},
      adBreakDone: function (info) {
        var err = info.error || 'ok';
        unityInstance.SendMessage('WebGLAdsManager', 'JS_BannerDone', err);
      }
    });
  },

  // 激励
  AdsShowRewarded: function () {
    adBreak({
      type: 'reward',
      name: 'webgl-reward',
      beforeAd: function () {
        unityInstance.SendMessage('WebGLAdsManager', 'JS_BeforeAd');
      },
      afterAd: function () {
        unityInstance.SendMessage('WebGLAdsManager', 'JS_AfterAd');
      },
      adBreakDone: function (info) {
        var res = (info.error ? 'fail' : 'ok') + '|' + (info.error || '');
        unityInstance.SendMessage('WebGLAdsManager', 'JS_RewardedDone', res);
      }
    });
  }

});