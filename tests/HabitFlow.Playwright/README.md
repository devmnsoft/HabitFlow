# Visual regression tests

The suite exercises the public persona at six representative viewports. Authenticated Free, account-owner and Super Administrator scenarios require CI-injected test users (`HABITFLOW_E2E_*`) and remain documented in `docs/V651_VISUAL_QA.md`; no credentials are stored here. Screenshots, traces, console errors and network failures are generated only under `test-results`/`playwright-report` artifacts and are gitignored.
