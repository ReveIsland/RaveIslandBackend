import { chromium } from "playwright";
import { execSync } from "node:child_process";
import { readFileSync } from "node:fs";
import path from "node:path";
import { fileURLToPath } from "node:url";

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const root = path.resolve(__dirname, "..");

function getPassword() {
  if (process.env.KEYCLOAK_ADMIN_PASSWORD) {
    return process.env.KEYCLOAK_ADMIN_PASSWORD;
  }

  try {
    const secrets = execSync(
      "dotnet user-secrets list --project RaveIsland.AppHost/RaveIsland.AppHost.csproj",
      { cwd: root, encoding: "utf8" },
    );
    const match = secrets.match(/Parameters:keycloak-password\s*=\s*(.+)/i);
    if (match?.[1]) {
      return match[1].trim();
    }
  } catch {
    // Fall through to appsettings lookup.
  }

  try {
    const appsettings = readFileSync(
      path.join(root, "RaveIsland.AppHost", "appsettings.Development.json"),
      "utf8",
    );
    const parsed = JSON.parse(appsettings);
    const value = parsed?.Parameters?.["keycloak-password"];
    if (value) {
      return value;
    }
  } catch {
    // Ignore.
  }

  throw new Error(
    "Keycloak admin password not found. Set KEYCLOAK_ADMIN_PASSWORD or add Parameters:keycloak-password to AppHost user secrets.",
  );
}

const adminUrl = process.env.KEYCLOAK_ADMIN_URL ?? "https://localhost:8080/admin";
const username = process.env.KEYCLOAK_ADMIN_USERNAME ?? "admin";
const password = getPassword();

const browser = await chromium.launch({ headless: false, slowMo: 150 });
const context = await browser.newContext({ ignoreHTTPSErrors: true });
const page = await context.newPage();

try {
  console.log(`Opening Keycloak admin console: ${adminUrl}`);
  await page.goto(adminUrl, { waitUntil: "domcontentloaded", timeout: 60_000 });

  await page.locator("#username").fill(username);
  await page.locator("#password").fill(password);
  await page.locator("#kc-login, input[type='submit'], button[type='submit']").first().click();

  await page.waitForLoadState("networkidle", { timeout: 30_000 }).catch(() => {});

  const currentUrl = page.url();
  const title = await page.title();
  const hasAdminUi =
    currentUrl.includes("/admin") &&
    !currentUrl.includes("/login") &&
    (await page.locator("text=master").count()) > 0;

  const screenshotPath = path.join(root, "keycloak-admin-login-result.png");
  await page.screenshot({ path: screenshotPath, fullPage: true });

  if (hasAdminUi || title.toLowerCase().includes("keycloak")) {
    console.log("Login appears successful.");
    console.log(`Current URL: ${currentUrl}`);
    console.log(`Screenshot: ${screenshotPath}`);
  } else {
    console.log("Login may have failed. Check the browser window and screenshot.");
    console.log(`Current URL: ${currentUrl}`);
    console.log(`Screenshot: ${screenshotPath}`);
    process.exitCode = 1;
  }

  await page.waitForTimeout(8_000);
} finally {
  await browser.close();
}
