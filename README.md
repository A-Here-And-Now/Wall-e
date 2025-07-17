Your implementation is impressive — it already demonstrates solid OOP design, reasonable separation of concerns, and domain alignment with fintech/wallet infrastructure. Let’s build on that foundation with a **structured roadmap** that introduces:

1. **Algorithmic depth**
2. **Design and refactor challenges**
3. **Data structure optimization**
4. **Concurrency and scale handling**
5. **Realistic extension of fintech features**

---

## 🔁 Phase 1: Refactor, Encapsulate, and Harden

### 🔹 Challenge 1: Abstract a Robust `Repository` Pattern

Move the `WalletService` to follow a proper repository pattern:

* Add `IWalletRepository`, split from service.
* Inject via `IWalletRepository` interface to improve SRP and testability.
* Swap implementation from `List<Wallet>` to an in-memory DB (e.g., `Dictionary<string, Wallet>`) or add simulated Redis support with TTLs.

💡 *Introduce async lock semantics to handle concurrent access across simulated threads.*

---

### 🔹 Challenge 2: Fix and Extend Daily Analytics

`DailyAverageTransactionValueAnalytic` has a few bugs:

* `NumTransactionsPerDate` is uninitialized.
* The averaging logic is off (`+1` is outside parentheses).

Refactor this to:

* Use a `SortedDictionary<DateTime, (decimal total, int count)>`.
* Allow querying for:

  * A specific date
  * A date range
  * Highest-volume day
  * Day with most transactions

✳️ This reinforces aggregation, sorting, and custom comparator logic.

---

## ⚙️ Phase 2: Add Features That Require Algorithmic Reasoning

### 🔹 Challenge 3: Add **Multi-Currency Wallet Support**

* Add currency codes to `Wallet`, `Transaction`, and `Analytics`.
* Ensure wallets store **per-currency balances**.
* Introduce an exchange rate service (with fake rates for now).
* Handle deposits in one currency, withdrawals in another.

This requires you to:

* Build nested data structures (`Dictionary<string, decimal>` inside analytics).
* Handle conversion logic on the fly.
* Refactor the alert system to work per-currency.

---

### 🔹 Challenge 4: Add a Fraud Detection Heuristic

Add a `FraudHeuristicEngine` with:

* **Rate limit detection** (e.g., >5 withdrawals in a 2-min window).
* **Round-trip detector**: user deposits and withdraws similar amounts within short spans.
* **Anomaly detector**: spikes in transaction size based on historical standard deviation.

You'll need:

* Sliding window logic (`Queue<T>`)
* Statistical calculations (mean, std dev, etc.)
* Possibly: Trie, bloom filter, or suffix tree if you want to get fancy.

🧠 This starts to involve **streaming**, **event-time logic**, and **anomaly algorithms**.

---

## 📤 Phase 3: Design-Level Growth + System-Level Decisions

### 🔹 Challenge 5: Add an `AlertSubscriber` System

Let alerts have **subscribers**:

* Wallets can register handlers (e.g., EmailAlertHandler, WebhookAlertHandler).
* When an alert is triggered, it gets dispatched to all relevant subscribers.

Force yourself to:

* Use interfaces with `IAlertHandler`.
* Add dependency injection.
* Batch alert delivery or throttle it (producer/consumer pattern).

🧠 Reinforces event-driven design, interface-based injection, and batching algorithms.

---

### 🔹 Challenge 6: Batch Analytics Recalculation Engine

Imagine that historic transaction data is re-ingested due to corrections.

Build a `RecomputeAnalyticsJob`:

* Replays the full list of transactions for a wallet.
* Allows partial recomputes (e.g., from a date forward).
* Validates that analytic values match expected values after recompute.

This is a challenge in:

* Idempotency
* Sliding window data application
* Snapshotting + reversion (optional)

---

## 🧰 Bonus: Add a Simple Test Harness

Add an internal CLI like:

```bash
> dotnet run --ingest "sample_data.json"
> dotnet run --query-analytics "user-001" --analytic "NetBalance"
> dotnet run --trigger-alerts "user-001"
```

This gives you a real-world way to orchestrate the code and mimic production CLI tooling.

---

## 📈 Stretch Goals

| Area                        | Example                                                  |
| --------------------------- | -------------------------------------------------------- |
| **Concurrency**             | Parallel transaction ingestion, using `Task` and locks   |
| **Sorting/Data structures** | Priority queue of alerts, LRU cache for queries          |
| **Scalability**             | Paginated APIs for transactions and analytics            |
| **Security/Validation**     | Add authentication and HMAC signatures on webhook alerts |

---

Would you like me to generate sample JSON data for ingestion, or bootstrapped files for any of the features above (e.g., AlertSubscriber system or RecomputeAnalyticsJob)?

Let's keep turning this into a world-class backend training ground.
