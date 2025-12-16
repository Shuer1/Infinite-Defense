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
    // 初始化广告配置默认值（避免未定义导致报错）
    window.webglAdConfig = window.webglAdConfig || {
      openAdSlot: '',
      bannerAdSlot: '',
      rewardedAdSlot: ''
    };
    if (typeof adBreak === 'undefined') {
      window.adsbygoogle = window.adsbygoogle || [];
      window.adBreak = function (o) { adsbygoogle.push(o); };
    }
  },

  WebGLAdsShowOpen: function () {
    const adType = "开屏广告";
    // 校验配置是否初始化
    if (!window.webglAdConfig) {
      const errorMsg = `[WebAdsBridge] ${adType}配置错误：webglAdConfig未初始化`;
      console.error(errorMsg);
      Module._safeSendUnityMessage('WebGLAdsManager', 'JS_OpenAdDone', 'config_error');
      return;
    }
    if (!window.webglAdConfig.openAdSlot) {
      const errorMsg = `[WebAdsBridge] ${adType}配置错误：openAdSlot未定义`;
      console.error(errorMsg);
      Module._safeSendUnityMessage('WebGLAdsManager', 'JS_OpenAdDone', 'config_error');
      return;
    }
    if (Module._openLock) {
      const warnMsg = `[WebAdsBridge] ${adType}请求被拦截：已有广告正在展示`;
      console.warn(warnMsg);
      return;
    }
    Module._openLock = true;
    adBreak({
      type: 'start',
      name: 'webgl-open',
      ad_slot: window.webglAdConfig.openAdSlot,
      beforeAd: function () { Module._safeSendUnityMessage('WebGLAdsManager', 'JS_BeforeAd'); },
      afterAd: function () { Module._safeSendUnityMessage('WebGLAdsManager', 'JS_AfterAd'); },
      adBreakDone: function (info) {
        Module._openLock = false;
        const res = info.error || 'ok';
        console.log(`[WebAdsBridge] ${adType}展示完成：${res}`);
        Module._safeSendUnityMessage('WebGLAdsManager', 'JS_OpenAdDone', res);
      }
    });
    // 8 秒兜底
    setTimeout(() => { if (Module._openLock) { Module._openLock = false; Module._safeSendUnityMessage('WebGLAdsManager', 'JS_OpenAdDone', 'timeout'); } }, 8000);
  },

  WebGLAdsShowBanner: function () {
    const adType = "横幅广告";
    // 校验配置是否初始化
    if (!window.webglAdConfig) {
      const errorMsg = `[WebAdsBridge] ${adType}配置错误：webglAdConfig未初始化`;
      console.error(errorMsg);
      Module._safeSendUnityMessage('WebGLAdsManager', 'JS_BannerDone', 'config_error');
      return;
    }
    if (!window.webglAdConfig.bannerAdSlot) {
      const errorMsg = `[WebAdsBridge] ${adType}配置错误：bannerAdSlot未定义`;
      console.error(errorMsg);
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
        const res = info.error || 'ok';
        console.log(`[WebAdsBridge] ${adType}展示完成：${res}`);
        Module._safeSendUnityMessage('WebGLAdsManager', 'JS_BannerDone', res);
      }
    });
  },

  WebGLAdsShowRewarded: function () {
    const adType = "激励广告";
    // 校验配置是否初始化
    if (!window.webglAdConfig) {
      const errorMsg = `[WebAdsBridge] ${adType}配置错误：webglAdConfig未初始化`;
      console.error(errorMsg);
      Module._safeSendUnityMessage('WebGLAdsManager', 'JS_RewardedDone', 'fail|config_error');
      return;
    }
    if (!window.webglAdConfig.rewardedAdSlot) {
      const errorMsg = `[WebAdsBridge] ${adType}配置错误：rewardedAdSlot未定义`;
      console.error(errorMsg);
      Module._safeSendUnityMessage('WebGLAdsManager', 'JS_RewardedDone', 'fail|config_error');
      return;
    }
    if (Module._rewardedLock) {
      const warnMsg = `[WebAdsBridge] ${adType}请求被拦截：已有广告正在展示`;
      console.warn(warnMsg);
      return;
    }
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
        console.log(`[WebAdsBridge] ${adType}展示完成：${res}（是否奖励：${info.isRewarded}）`);
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
  },

  // 新增：动态设置广告位ID
  SetAdSlot: function (adTypePtr, slotIdPtr) {
    const adType = UTF8ToString(adTypePtr);
    const slotId = UTF8ToString(slotIdPtr);
    if (!window.webglAdConfig) window.webglAdConfig = {};
    switch (adType) {
      case "open":
        window.webglAdConfig.openAdSlot = slotId;
        break;
      case "banner":
        window.webglAdConfig.bannerAdSlot = slotId;
        break;
      case "rewarded":
        window.webglAdConfig.rewardedAdSlot = slotId;
        break;
      default:
        console.warn(`[WebAdsBridge] 未知广告类型：${adType}`);
    }
    console.log(`[WebAdsBridge] 动态设置${adType}广告位ID：${slotId}`);
  },

  // 新增：获取当前广告位配置
  GetAdSlot: function (adTypePtr) {
    const adType = UTF8ToString(adTypePtr);
    if (!window.webglAdConfig) return 0;
    let slotId = "";
    switch (adType) {
      case "open":
        slotId = window.webglAdConfig.openAdSlot || "";
        break;
      case "banner":
        slotId = window.webglAdConfig.bannerAdSlot || "";
        break;
      case "rewarded":
        slotId = window.webglAdConfig.rewardedAdSlot || "";
        break;
    }
    return allocate(intArrayFromString(slotId), ALLOC_NORMAL);
  }
});