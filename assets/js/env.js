export const APP_VERSION = "v2.3.4-Audit-Fix-Callable-Cache-Layout";

const localHosts = new Set(["localhost", "127.0.0.1", "::1"]);
const detectedLocal = localHosts.has(location.hostname);

const host = location.hostname;
const explicitEnv = import.meta.env?.VITE_APP_ENV || "";
export const APP_ENV = explicitEnv || (detectedLocal ? "local" : (host.includes("--") || host.includes("staging") || host.includes("preview") ? "staging" : "production"));
export const IS_DEVELOPMENT = APP_ENV === "local" || APP_ENV === "development";
export const IS_PRODUCTION = APP_ENV === "production";

export const APP_CHECK_ENABLED = String(import.meta.env?.VITE_APP_CHECK_ENABLED || "false") === "true";
export const APP_CHECK_SITE_KEY = import.meta.env?.VITE_APP_CHECK_SITE_KEY || "";
export const APP_CHECK_DEBUG_TOKEN = import.meta.env?.VITE_APP_CHECK_DEBUG_TOKEN || "";
