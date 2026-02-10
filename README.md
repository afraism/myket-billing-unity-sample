# Myket Billing Unity Sample

A Unity sample project for integrating **Myket Billing** on Android using Gradle templates and the Myket Unity plugin.

## Overview

This project includes:
- Myket billing C# wrapper (`MyketIAB`)
- Event-based callback manager (`IABEventManager`)
- Purchase and SKU models (`MyketPurchase`, `MyketSkuInfo`)
- Android Gradle templates for Myket dependencies/repositories
- A demo scene for quickly testing billing flows

## Project Structure

- `Assets/MyketIAB/` → Billing wrapper, event manager, models, and helper utilities
- `Assets/MyketIAB/Demo/` → Demo scene and testing scripts
- `Assets/Plugins/Android/` → `AndroidManifest.xml` and Gradle templates
- `ProjectSettings/` → Unity project configuration

## Setup

1. Open the project in Unity.
2. Switch build target to **Android**.
3. Ensure custom Gradle templates are enabled (if your Unity version requires manual enablement):
   - Main Gradle template
   - Launcher Gradle template
   - Settings Gradle template
4. Set your application package name in Unity Player Settings.
5. Replace demo product IDs in `Assets/MyketIAB/Demo/IABTestUI.cs` with your real Myket SKU IDs.
6. Replace the demo public key passed to `MyketIAB.init(...)` with your own key from Myket developer portal.

## Basic Usage Flow

1. Initialize billing:
   - `MyketIAB.init(publicKey)`
2. Query products:
   - `MyketIAB.querySkuDetails(skus)`
3. Restore existing purchases:
   - `MyketIAB.queryPurchases()`
4. Purchase item:
   - `MyketIAB.purchaseProduct(sku)`
5. Consume consumables:
   - `MyketIAB.consumeProduct(sku)`

Subscribe to `IABEventManager` events to receive callbacks for each operation (success/failure).

## Demo

Open and run:
- `Assets/MyketIAB/Demo/IABTestScene.unity`

The demo UI provides buttons for initialize, query inventory, query SKU details, query purchases, purchase, and consume actions.

## Troubleshooting

- If callbacks are not fired, ensure callback receiver object names are not changed and event listeners are subscribed in `OnEnable` and removed in `OnDisable`.
- Test on a real Android device (billing behavior is not fully testable in Unity Editor).
- Confirm your SKUs and public key exactly match your Myket portal configuration.

## Notes

- Use `enableLogging(true)` only for debug/testing builds.
- Keep your production receipt validation on backend side for secure entitlement granting.

improve by AFRA
