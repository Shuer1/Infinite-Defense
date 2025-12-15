mergeInto(LibraryManager.library, {
  // 安全调用 Unity 消息
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
    if (typeof unityInstance === 'undefined') {
      console.warn('unityInstance is not initialized when ads init');
    }
  },

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

  WebGLAdsShowRewarded: function () {
    adBreak({
      type: 'reward', 
      name: 'webgl-reward',
      ad_slot: window.webglAdConfig.rewardedAdSlot,
      beforeAd: function () { Module._safeSendUnityMessage('WebGLAdsManager', 'JS_BeforeAd'); },
      afterAd: function () { Module._safeSendUnityMessage('WebGLAdsManager', 'JS_AfterAd'); },
      adBreakDone: function (info) {
        const isSuccess = !info.error && info.isRewarded === true;
        const res = isSuccess ? 'ok|' : 'fail|' + (info.error || (info.isRewarded === false ? 'user_abort' : 'unknown'));
        Module._safeSendUnityMessage('WebGLAdsManager', 'JS_RewardedDone', res);
      }
    });
  },

  // ===== 设备 ID 相关 =====
  SetUserAdId: function (udidPtr) {
    var udid = UTF8ToString(udidPtr);
    if (typeof setUserAdId === 'function') {
      try { setUserAdId(udid); } 
      catch (e) { console.error('[WebAdsBridge] setUserAdId failed:', e); }
    } else {
      console.warn('[WebAdsBridge] setUserAdId not found on page');
    }
  }
});