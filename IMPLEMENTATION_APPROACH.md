# Unity Implementation Approach for Myket Billing

This repository is already a Unity sample project with a working Myket Android billing integration. Use this document as a production implementation plan for integrating it into your own game/app.

## 1) What is already in the repository

### Core plugin wrapper
- `Assets/MyketIAB/MyketIAB.cs`
  - Static C# bridge to Android Java plugin (`ir.myket.billingclient.MyketIABPlugin`).
  - Exposes API methods:
    - `init(publicKey)`
    - `queryInventory(skus)`
    - `querySkuDetails(skus)`
    - `queryPurchases()`
    - `purchaseProduct(sku, payload)`
    - `consumeProduct(sku)` / `consumeProducts(skus)`
    - `areSubscriptionsSupported()`
    - `enableLogging(bool)`

### Event system
- `Assets/MyketIAB/MyketIABEventManager.cs`
  - Receives native callbacks and raises strongly-typed C# events.
  - Converts JSON responses to C# models:
    - `MyketPurchase`
    - `MyketSkuInfo`

### Data models
- `Assets/MyketIAB/MyketPurchase.cs`
- `Assets/MyketIAB/MyketSkuInfo.cs`

### Demo scene and scripts
- `Assets/MyketIAB/Demo/IABTestScene.unity`
- `Assets/MyketIAB/Demo/IABTestUI.cs`
- `Assets/MyketIAB/Demo/MyketIABEventListener.cs`

### Android build integration
- `Assets/Plugins/Android/launcherTemplate.gradle`
  - Includes `com.github.myketstore:myket-billing-unity:unity-1.6`
- `Assets/Plugins/Android/settingsTemplate.gradle`
  - Adds Myket Maven + JitPack repositories
- `Assets/Plugins/Android/gradleTemplate.properties`
  - AndroidX + Unity gradle settings

## 2) Recommended production architecture

Do **not** keep billing calls inside UI components. Build this structure:

1. **BillingFacade (domain service)**
   - Single entry point for purchase flows.
   - Wraps all `MyketIAB` calls.
2. **BillingEventRouter**
   - Subscribes/unsubscribes to `IABEventManager` events once.
   - Converts plugin event payloads into your game events.
3. **ProductCatalogService**
   - Holds SKU IDs and metadata map.
   - Fetches from `querySkuDetails` at startup/shop-open.
4. **EntitlementService**
   - Applies unlocks after successful purchase.
   - Handles non-consumable vs consumable vs subscription behavior.
5. **ReceiptValidationBackend (recommended)**
   - Unity client sends purchase token + productId + originalJson.
   - Server verifies purchase and grants durable entitlements.

This separation keeps UI simple and prevents logic duplication.

## 3) Step-by-step integration plan

## Phase A — Prepare store products
1. Create all SKUs in Myket portal:
   - Consumables
   - Non-consumables
   - Subscriptions
2. Keep a canonical SKU list in one C# file (avoid string literals spread across code).

## Phase B — Unity project setup
1. Keep plugin files under `Assets/MyketIAB` and `Assets/Plugins/Android`.
2. In Unity Player Settings:
   - Platform: Android
   - Use custom main manifest/gradle templates (already present).
3. Set your application ID/package name consistently with your store listing.

## Phase C — Billing bootstrap
1. Create `BillingBootstrap` MonoBehaviour in your startup scene.
2. On app startup:
   - Subscribe to all billing events.
   - Call `MyketIAB.init(publicKey)` once.
3. On app quit (or shutdown path):
   - Unsubscribe events.
   - Call `MyketIAB.unbindService()`.

## Phase D — Catalog and ownership sync
1. After `billingSupportedEvent`:
   - Call `querySkuDetails(skus)` for prices/titles.
   - Call `queryPurchases()` to restore ownership.
2. Cache latest SKU info for shop UI.
3. Build ownership state from `queryPurchasesSucceededEvent`.

## Phase E — Purchase flow
1. User taps buy.
2. Check local guard rails:
   - Billing initialized
   - SKU exists
   - No in-flight purchase for same SKU
3. Call `purchaseProduct(sku, payload)`.
4. On `purchaseSucceededEvent`:
   - Send receipt data to backend for validation.
   - Grant entitlement after validation.
5. On failure:
   - Map error to user-friendly message.

## Phase F — Consumption flow (consumables only)
1. Grant item only after verified purchase.
2. Consume via `consumeProduct(sku)` once granted and persisted.
3. If consume fails, queue a retry job.

## 4) Critical production rules

1. **Initialize only once per app lifecycle.**
2. **Idempotent grants**: use orderId/purchaseToken to avoid double rewards.
3. **Do not trust client-only purchase success** for valuable items.
4. **Restore purchases** every launch or account change.
5. **Gate consumables vs non-consumables** by product type config.
6. **Use high-detail logging only in debug builds** (`enableLogging(true)`).

## 5) Suggested code skeleton (high-level)

```csharp
public sealed class BillingFacade : MonoBehaviour
{
    public void Initialize(string publicKey) => MyketIAB.init(publicKey);
    public void QueryCatalog(string[] skus) => MyketIAB.querySkuDetails(skus);
    public void Restore() => MyketIAB.queryPurchases();
    public void Buy(string sku, string payload = "") => MyketIAB.purchaseProduct(sku, payload);
    public void Consume(string sku) => MyketIAB.consumeProduct(sku);
}
```

```csharp
public sealed class BillingEventRouter : MonoBehaviour
{
    private void OnEnable()
    {
        IABEventManager.purchaseSucceededEvent += OnPurchaseSucceeded;
        IABEventManager.purchaseFailedEvent += OnPurchaseFailed;
        IABEventManager.queryPurchasesSucceededEvent += OnRestoreSucceeded;
    }

    private void OnDisable()
    {
        IABEventManager.purchaseSucceededEvent -= OnPurchaseSucceeded;
        IABEventManager.purchaseFailedEvent -= OnPurchaseFailed;
        IABEventManager.queryPurchasesSucceededEvent -= OnRestoreSucceeded;
    }
}
```

## 6) Release checklist

- [ ] All SKUs created and active in Myket console.
- [ ] Public key configured correctly for release build.
- [ ] Purchase success tested on real Android device.
- [ ] Consumable retry logic tested with offline scenarios.
- [ ] App relaunch restore flow tested.
- [ ] Subscription purchase/renew/cancel behavior validated.
- [ ] Backend validation and idempotent entitlement checks in place.

## 7) Testing strategy

1. **Editor tests (logic-only)**
   - SKU mapping
   - Idempotent entitlement rules
   - Error mapping
2. **On-device integration tests**
   - Init success/failure
   - Query SKU details
   - Purchase success/failure
   - Consumption success/failure
   - Restore across reinstall/login
3. **Chaos tests**
   - Kill app mid-purchase
   - Network loss during purchase callback
   - Duplicate callbacks (ensure no double grant)

## 8) Migration plan from sample to production

1. Keep sample scene for diagnostics only (internal QA build).
2. Build your own billing service scripts in a separate folder (e.g., `Assets/Scripts/Billing`).
3. Replace hardcoded keys and SKUs with secure config.
4. Add backend receipt verification endpoint.
5. Add analytics events for each billing stage.

---

If you want, next step I can generate a **drop-in production folder structure** (`BillingFacade`, `BillingBootstrap`, `EntitlementService`, `ReceiptApiClient`, `ShopViewModel`) directly in this repo with starter scripts.
