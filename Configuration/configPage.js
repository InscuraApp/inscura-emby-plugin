define(["emby-input", "emby-button", "emby-checkbox", "emby-select"], function () {
    var pluginId = "477acb51-8e08-4664-8f1e-f56391796cf1";
    var languageStorageKey = "emby.plugin.inscura.language";
    var translations = {
        en: {
            language: "Language",
            english: "English",
            chinese: "Simplified Chinese",
            intro: "Import movie metadata, people, artwork, media stream details, and images from the Inscura local API into Emby.",
            trailerNotice: "Remote trailers are currently imported only when they are YouTube URLs. For local trailers or non-YouTube online trailers, export NFO files from the Inscura App and let Emby scan them.",
            apiBaseUrl: "Inscura API URL",
            apiBaseUrlDescription: "Example: http://192.168.10.198:28687",
            apiToken: "API Token",
            apiTokenDescription: "Set this only when the Inscura API requires token authentication. Leave it empty when authMode is none.",
            searchLimit: "Search result limit",
            requestTimeoutSeconds: "Request timeout (seconds)",
            enableMetadataProvider: "Enable movie metadata provider",
            enableImageProvider: "Enable image provider",
            enableAutomaticImageImport: "Automatically save missing Emby artwork types",
            enablePersonProvider: "Enable person metadata and image provider",
            enableYouTubeTrailers: "Import YouTube trailers",
            includePreviewAsThumb: "Use Inscura preview images as Thumb candidates",
            includeGalleryBackdrops: "Use fanart and screenshots as Backdrop/Screenshot candidates",
            includeMediaStreams: "Import media stream details from Inscura",
            save: "Save"
        },
        "zh-CN": {
            language: "界面语言",
            english: "英文",
            chinese: "简体中文",
            intro: "从 Inscura 本地 API 为 Emby 电影库导入元数据、演员、图片和媒体流信息。",
            trailerNotice: "远程预告片当前只导入 YouTube 地址。非 YouTube 本地或在线预告片请通过 Inscura App 的 NFO 功能导出后交给 Emby 扫描。",
            apiBaseUrl: "Inscura API 地址",
            apiBaseUrlDescription: "示例：http://192.168.10.198:28687",
            apiToken: "API Token",
            apiTokenDescription: "仅当 Inscura API 需要 token 鉴权时填写；authMode 为 none 时可留空。",
            searchLimit: "搜索候选数量",
            requestTimeoutSeconds: "请求超时（秒）",
            enableMetadataProvider: "启用电影元数据刮削",
            enableImageProvider: "启用图片导入",
            enableAutomaticImageImport: "自动补齐 Emby 缺失的图片类型",
            enablePersonProvider: "启用演员元数据和头像导入",
            enableYouTubeTrailers: "导入 YouTube 预告片",
            includePreviewAsThumb: "将 Inscura 预览图作为 Thumb 图片候选",
            includeGalleryBackdrops: "将图库图片和截图作为 Backdrop/Screenshot 候选",
            includeMediaStreams: "导入 Inscura 媒体流信息",
            save: "保存"
        }
    };

    function InscuraConfigurationPage(view) {
        this.view = view;
        this.submitHandler = this.onSubmit.bind(this);
        this.languageChangeHandler = this.onLanguageChange.bind(this);
        this.resumeHandler = this.onResume.bind(this);
        view.querySelector("#InscuraConfigForm").addEventListener("submit", this.submitHandler);
        view.querySelector("#selectLanguage").addEventListener("change", this.languageChangeHandler);
        view.addEventListener("viewshow", this.resumeHandler);
        view.addEventListener("pageshow", this.resumeHandler);
        setTimeout(this.resumeHandler, 0);
    }

    InscuraConfigurationPage.prototype.onResume = function () {
        this.applyLanguage(this.getInitialLanguage());
        this.loadConfiguration();
    };

    InscuraConfigurationPage.prototype.destroy = function () {
        this.view.querySelector("#InscuraConfigForm").removeEventListener("submit", this.submitHandler);
        this.view.querySelector("#selectLanguage").removeEventListener("change", this.languageChangeHandler);
        this.view.removeEventListener("viewshow", this.resumeHandler);
        this.view.removeEventListener("pageshow", this.resumeHandler);
    };

    InscuraConfigurationPage.prototype.byId = function (id) {
        return this.view.querySelector("#" + id);
    };

    InscuraConfigurationPage.prototype.getStoredLanguage = function () {
        try {
            return localStorage.getItem(languageStorageKey);
        } catch (err) {
            return null;
        }
    };

    InscuraConfigurationPage.prototype.storeLanguage = function (language) {
        try {
            localStorage.setItem(languageStorageKey, language);
        } catch (err) {
        }
    };

    InscuraConfigurationPage.prototype.normalizeLanguage = function (language) {
        return language && language.toLowerCase().indexOf("zh") === 0 ? "zh-CN" : "en";
    };

    InscuraConfigurationPage.prototype.getInitialLanguage = function () {
        var storedLanguage = this.getStoredLanguage();
        if (storedLanguage === "en" || storedLanguage === "zh-CN") {
            return storedLanguage;
        }

        var browserLanguages = navigator.languages && navigator.languages.length ? navigator.languages : [navigator.language || "en"];
        return this.normalizeLanguage(browserLanguages[0]);
    };

    InscuraConfigurationPage.prototype.setText = function (id, value) {
        var element = this.byId(id);
        if (element) {
            element.textContent = value;
        }
    };

    InscuraConfigurationPage.prototype.applyLanguage = function (language) {
        var activeLanguage = language === "zh-CN" ? "zh-CN" : "en";
        var text = translations[activeLanguage];

        this.byId("selectLanguage").value = activeLanguage;
        this.setText("labelSelectLanguage", text.language);
        this.setText("languageOptionEn", text.english);
        this.setText("languageOptionZh", text.chinese);
        this.setText("txtIntro", text.intro);
        this.setText("txtTrailerNotice", text.trailerNotice);
        this.setText("labelApiBaseUrl", text.apiBaseUrl);
        this.setText("descApiBaseUrl", text.apiBaseUrlDescription);
        this.setText("labelApiToken", text.apiToken);
        this.setText("descApiToken", text.apiTokenDescription);
        this.setText("labelSearchLimit", text.searchLimit);
        this.setText("labelRequestTimeoutSeconds", text.requestTimeoutSeconds);
        this.setText("labelEnableMetadataProvider", text.enableMetadataProvider);
        this.setText("labelEnableImageProvider", text.enableImageProvider);
        this.setText("labelEnableAutomaticImageImport", text.enableAutomaticImageImport);
        this.setText("labelEnablePersonProvider", text.enablePersonProvider);
        this.setText("labelEnableYouTubeTrailers", text.enableYouTubeTrailers);
        this.setText("labelIncludePreviewAsThumb", text.includePreviewAsThumb);
        this.setText("labelIncludeGalleryBackdrops", text.includeGalleryBackdrops);
        this.setText("labelIncludeMediaStreams", text.includeMediaStreams);
        this.setText("labelSave", text.save);
    };

    InscuraConfigurationPage.prototype.onLanguageChange = function () {
        var language = this.byId("selectLanguage").value === "zh-CN" ? "zh-CN" : "en";
        this.storeLanguage(language);
        this.applyLanguage(language);
    };

    InscuraConfigurationPage.prototype.loadConfiguration = function () {
        var page = this;
        showLoading();
        ApiClient.getPluginConfiguration(pluginId).then(function (config) {
            page.byId("txtApiBaseUrl").value = config.ApiBaseUrl || "http://192.168.10.198:28687";
            page.byId("txtApiToken").value = config.ApiToken || "";
            page.byId("txtSearchLimit").value = config.SearchLimit || 10;
            page.byId("txtRequestTimeoutSeconds").value = config.RequestTimeoutSeconds || 15;
            page.byId("chkEnableMetadataProvider").checked = config.EnableMetadataProvider !== false;
            page.byId("chkEnableImageProvider").checked = config.EnableImageProvider !== false;
            page.byId("chkEnableAutomaticImageImport").checked = config.EnableAutomaticImageImport !== false;
            page.byId("chkEnablePersonProvider").checked = config.EnablePersonProvider !== false;
            page.byId("chkEnableYouTubeTrailers").checked = config.EnableYouTubeTrailers !== false;
            page.byId("chkIncludePreviewAsThumb").checked = config.IncludePreviewAsThumb !== false;
            page.byId("chkIncludeGalleryBackdrops").checked = config.IncludeGalleryBackdrops !== false;
            page.byId("chkIncludeMediaStreams").checked = config.IncludeMediaStreams !== false;
            hideLoading();
        });
    };

    InscuraConfigurationPage.prototype.onSubmit = function (event) {
        var page = this;
        event.preventDefault();
        showLoading();
        ApiClient.getPluginConfiguration(pluginId).then(function (config) {
            config.ApiBaseUrl = page.byId("txtApiBaseUrl").value;
            config.ApiToken = page.byId("txtApiToken").value;
            config.SearchLimit = parseInt(page.byId("txtSearchLimit").value || "10", 10);
            config.RequestTimeoutSeconds = parseInt(page.byId("txtRequestTimeoutSeconds").value || "15", 10);
            config.EnableMetadataProvider = page.byId("chkEnableMetadataProvider").checked;
            config.EnableImageProvider = page.byId("chkEnableImageProvider").checked;
            config.EnableAutomaticImageImport = page.byId("chkEnableAutomaticImageImport").checked;
            config.EnablePersonProvider = page.byId("chkEnablePersonProvider").checked;
            config.EnableYouTubeTrailers = page.byId("chkEnableYouTubeTrailers").checked;
            config.IncludePreviewAsThumb = page.byId("chkIncludePreviewAsThumb").checked;
            config.IncludeGalleryBackdrops = page.byId("chkIncludeGalleryBackdrops").checked;
            config.IncludeMediaStreams = page.byId("chkIncludeMediaStreams").checked;

            ApiClient.updatePluginConfiguration(pluginId, config).then(function (result) {
                hideLoading();
                processUpdateResult(result);
            });
        });
        return false;
    };

    function showLoading() {
        if (window.Dashboard && Dashboard.showLoadingMsg) {
            Dashboard.showLoadingMsg();
        }
    }

    function hideLoading() {
        if (window.Dashboard && Dashboard.hideLoadingMsg) {
            Dashboard.hideLoadingMsg();
        }
    }

    function processUpdateResult(result) {
        if (window.Dashboard && Dashboard.processPluginConfigurationUpdateResult) {
            Dashboard.processPluginConfigurationUpdateResult(result);
        }
    }

    return InscuraConfigurationPage;
});
