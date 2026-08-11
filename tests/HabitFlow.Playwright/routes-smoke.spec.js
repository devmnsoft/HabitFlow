import { test, expect } from '@playwright/test';
const publicRoutes=['/','/plans','/privacy','/help'];
const privateRoutes=['/dashboard','/my-day','/habits','/goals','/progress/calendar','/reports','/habit-library','/account/privacy','/account/plan/usage','/notifications','/reminders'];
for(const route of publicRoutes)test(`public route ${route}`,async({page})=>{const errors=[];page.on('pageerror',e=>errors.push(e.message));const response=await page.goto(route);expect(response.status()).toBeLessThan(400);expect(errors).toEqual([])});
for(const route of privateRoutes)test(`authenticated route ${route}`,async({page})=>{test.skip(!process.env.HABITFLOW_AUTH_STORAGE,'auth storage required');const response=await page.goto(route);expect(response.status()).toBeLessThan(400);await expect(page).not.toHaveURL(/\/login(?:\?|$)/)});
