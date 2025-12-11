mergeInto(LibraryManager.library, {
  // 新增：安全调用 Unity 消息（避免 unityInstance 未初始化）
  _safeSendUnityMessage: function (gameObject, methodName, param = '') {
    if (typeof unityInstance !== 'undefined' && unityInstance.SendMessage) {
      try {
        unityInstance.SendMessage(gameObject, methodName, param);
      } catch (e) {
        console.error('SendMessage failed:', e);
      }
    } else {
      console.warn('unityInstance not ready, cannot send message:', methodName);
    }
  },

  WebGLAdsInit: function () {
    if (typeof adBreak === 'undefined') {
      window.adsbygoogle = window.adsbygoogle || [];
      window.adBreak = function (o) { adsbygoogle.push(o); };
    }
    // 初始化时校验 unityInstance（提前暴露风险）
    if (typeof unityInstance === 'undefined') {
      console.warn('unityInstance is not initialized when ads init');
    }
  },

  // 开屏广告（替换为 safeSendUnityMessage）
  WebGLAdsShowOpen: function () {
    if (!window.webglAdConfig?.openAdSlot) {
      console.error('WebGL Open Ad Slot is missing!');
      Module._safeSendUnityMessage('WebGLAdsManager', 'JS_OpenAdDone', 'config_error');
      return;
    }
    adBreak({
      type: 'start', 
      name: 'webgl-open', 
      ad_slot: window.webglAdConfig.openAdSlot,
      beforeAd: function () { Module._safeSendUnityMessage('WebGLAdsManager', 'JS_BeforeAd'); },
      afterAd: function () { Module._safeSendUnityMessage('WebGLAdsManager', 'JS_AfterAd'); },
      adBreakDone: function (info) {
        Module._safeSendUnityMessage('WebGLAdsManager', 'JS_OpenAdDone', info.error || 'ok');
      }
    });
  },

  // 横幅广告（同步替换）
  WebGLAdsShowBanner: function () {
    if (!window.webglAdConfig?.bannerAdSlot) {
      console.error('WebGL Banner Ad Slot is missing!');
      Module._safeSendUnityMessage('WebGLAdsManager', 'JS_BannerDone', 'config_error');
      return;
    }
    adBreak({
      type: 'sticky', 
      name: 'webgl-banner',
      ad_slot: window.webglAdConfig.bannerAdSlot,
      beforeAd: function () {},
      afterAd: function () {},
      adBreakDone: function (info) {
        Module._safeSendUnityMessage('WebGLAdsManager', 'JS_BannerDone', info.error || 'ok');
      }
    });
  },

  // 激励广告（同步替换）
  WebGLAdsShowRewarded: function () {
    adBreak({
      type: 'reward', 
      name: 'webgl-reward',
      ad_slot: window.webglAdConfig.rewardedAdSlot, // 补充激励广告单元ID（需index.html配置）
      beforeAd: function () { Module._safeSendUnityMessage('WebGLAdsManager', 'JS_BeforeAd'); },
      afterAd: function () { Module._safeSendUnityMessage('WebGLAdsManager', 'JS_AfterAd'); },
      adBreakDone: function (info) {
        const isSuccess = !info.error && info.isRewarded === true;
        const res = isSuccess ? 'ok|' : 'fail|' + (info.error || (info.isRewarded === false ? 'user_abort' : 'unknown'));
        Module._safeSendUnityMessage('WebGLAdsManager', 'JS_RewardedDone', res);
      }
    });
  }

});