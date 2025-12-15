mergeInto(LibraryManager.library, {
  _safeSendUnityMessage: function (go, method, param = '') {
    if (typeof unityInstance !== 'undefined' && unityInstance.SendMessage) {
      try { unityInstance.SendMessage(go, method, param); } catch (e) {
        console.error('[WebAdsBridge] SendMessage failed:', e);
      }
    } else {
      console.warn('[WebAdsBridge] unityInstance not ready, skip', method);
    }
  },

  WebGLAdsInit: function () {
    // 隐私合规示例
    if (typeof canShowAds === 'boolean' && !canShowAds) {
      console.log('[WebAdsBridge] User consent denied, ads init skipped.');
      return;
    }
    if (typeof adBreak === 'undefined') {
      window.adsbygoogle = window.adsbygoogle || [];
      window.adBreak = function (o) { adsbygoogle.push(o); };
    }
  },

  WebGLAdsShowOpen: function () {
    if (!window.webglAdConfig?.openAdSlot) {
      Module._safeSendUnityMessage('WebGLAdsManager', 'JS_OpenAdDone', 'config_error');
      return;
    }
    if (Module._openLock) return;   // 防重点
    Module._openLock = true;
    adBreak({
      type: 'start',
      name: 'webgl-open',
      ad_slot: window.webglAdConfig.openAdSlot,
      beforeAd: function () { Module._safeSendUnityMessage('WebGLAdsManager', 'JS_BeforeAd'); },
      afterAd: function () { Module._safeSendUnityMessage('WebGLAdsManager', 'JS_AfterAd'); },
      adBreakDone: function (info) {
        Module._openLock = false;
        Module._safeSendUnityMessage('WebGLAdsManager', 'JS_OpenAdDone', info.error || 'ok');
      }
    });
    // 8 秒兜底
    setTimeout(() => { if (Module._openLock) { Module._openLock = false; Module._safeSendUnityMessage('WebGLAdsManager', 'JS_OpenAdDone', 'timeout'); } }, 8000);
  },

  WebGLAdsShowBanner: function () {
    if (!window.webglAdConfig?.bannerAdSlot) {
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
    if (!window.webglAdConfig?.rewardedAdSlot) {
      Module._safeSendUnityMessage('WebGLAdsManager', 'JS_RewardedDone', 'fail|config_error');
      return;
    }
    if (Module._rewardedLock) return;
    Module._rewardedLock = true;
    adBreak({
      type: 'reward',
      name: 'webgl-reward',
      ad_slot: window.webglAdConfig.rewardedAdSlot,
      beforeAd: function () { Module._safeSendUnityMessage('WebGLAdsManager', 'JS_BeforeAd'); },
      afterAd: function () { Module._safeSendUnityMessage('WebGLAdsManager', 'JS_AfterAd'); },
      adBreakDone: function (info) {
        Module._rewardedLock = false;
        const isSuccess = !info.error && info.isRewarded === true;
        const res = (isSuccess ? 'ok|' : 'fail|') + (info.error || (info.isRewarded === false ? 'user_abort' : 'unknown'));
        Module._safeSendUnityMessage('WebGLAdsManager', 'JS_RewardedDone', res);
      }
    });
    setTimeout(() => { if (Module._rewardedLock) { Module._rewardedLock = false; Module._safeSendUnityMessage('WebGLAdsManager', 'JS_RewardedDone', 'fail|timeout'); } }, 15000);
  },

  SetUserAdId: function (udidPtr) {
    const udid = UTF8ToString(udidPtr);
    if (typeof setUserAdId === 'function') {
      try { setUserAdId(udid); } catch (e) { console.error('[WebAdsBridge] setUserAdId failed:', e); }
    } else {
      console.warn('[WebAdsBridge] setUserAdId not found on page');
    }
  }
});