export const APP_VERSION = "2.0-security";

const localHosts = new Set(["localhost", "127.0.0.1", "::1"]);
const detectedLocal = localHosts.has(location.hostname);

export const APP_ENV = detectedLocal ? "development" : "production";
export const IS_DEVELOPMENT = APP_ENV === "development";
export const IS_PRODUCTION = APP_ENV === "production";

export const APP_CHECK_ENABLED = String(import.meta.env?.VITE_APP_CHECK_ENABLED || "false") === "true";
export const APP_CHECK_SITE_KEY = import.meta.env?.VITE_APP_CHECK_SITE_KEY || "";
export const APP_CHECK_DEBUG_TOKEN = import.meta.env?.VITE_APP_CHECK_DEBUG_TOKEN || "";
