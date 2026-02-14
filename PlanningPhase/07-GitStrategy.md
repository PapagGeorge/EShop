# 07 - Git Strategy & Practice Scenarios

## Branching Strategy: GitFlow (Simplified)

Στον πραγματικό κόσμο υπάρχουν 2 δημοφιλείς στρατηγικές:
- **GitFlow:** Πιο structured, πολλά branches, ιδανικό για scheduled releases
- **Trunk-Based:** Πιο απλό, μικρά branches, συνεχές merge στο main

Εμείς θα δουλέψουμε με **Simplified GitFlow** γιατί σου δίνει
περισσότερη εξάσκηση στο branching/merging.

---

## Branch Types

```
main (production-ready code)
│
├── develop (integration branch — εδώ μαζεύονται τα features)
│   │
│   ├── feature/identity-service
│   ├── feature/ordering-domain
│   ├── feature/ordering-application
│   ├── feature/ordering-infrastructure
│   ├── feature/ordering-api
│   ├── feature/catalog-service
│   ├── feature/api-gateway
│   └── feature/docker-compose
│
├── bugfix/fix-order-validation    (bug fixes στο develop)
│
├── release/v1.0                   (preparation for release)
│
└── hotfix/critical-auth-fix       (emergency fix στο main)
```

### Κανόνες

| Branch | Κόβεται από | Merge πίσω σε | Πότε |
|--------|------------|---------------|------|
| `feature/*` | develop | develop | Κάθε νέο feature/phase |
| `bugfix/*` | develop | develop | Bug σε κώδικα στο develop |
| `release/*` | develop | main + develop | Έτοιμο για release |
| `hotfix/*` | main | main + develop | Critical bug σε production |

---

## Commit Message Convention (Conventional Commits)

Format:
```
<type>(<scope>): <description>

[optional body]
```

### Types
| Type | Πότε |
|------|------|
| `feat` | Νέο feature |
| `fix` | Bug fix |
| `refactor` | Αλλαγή κώδικα χωρίς αλλαγή behavior |
| `test` | Προσθήκη/αλλαγή tests |
| `docs` | Documentation |
| `chore` | Build, config, dependencies |
| `style` | Formatting, missing semicolons (όχι logic change) |

### Παραδείγματα
```
feat(ordering): add CreateOrderCommandHandler

fix(identity): fix JWT expiration not being set correctly

refactor(ordering): extract address validation to value object

test(ordering): add unit tests for order cancellation

chore: add docker-compose with SQL Server and RabbitMQ

docs: add API contracts documentation
```

### Κανόνες
- Imperative mood: "add" όχι "added" ή "adds"
- Lowercase
- Χωρίς τελεία στο τέλος
- Max 72 χαρακτήρες στην πρώτη γραμμή
- Body για εξήγηση του "γιατί", όχι του "τί"

---

## Visual Studio Git UI — Τι θα χρησιμοποιήσουμε

Στο Visual Studio, τα βασικά panels:

1. **Git Changes** (View → Git Changes)
   - Stage/Unstage files
   - Write commit message
   - Commit
   - Push/Pull

2. **Git Repository** (View → Git Repository)
   - Branch visualization (graph)
   - History ανά branch
   - Merge, Rebase
   - Cherry-pick

3. **Branch dropdown** (bottom-left status bar)
   - Create new branch
   - Switch branches
   - View all local/remote branches

---

## Πρακτικά Σενάρια που θα εξασκήσουμε

Κάθε σενάριο θα το κάνουμε κατά τη διάρκεια της υλοποίησης,
σε πραγματικές συνθήκες — όχι σε dummy repo.

### Σενάριο 1: Basic Feature Branch Flow
**Πότε:** Phase 2 (Identity Service)
**Τι κάνουμε:**
1. Από develop, δημιουργούμε branch `feature/identity-service`
2. Κάνουμε commits καθώς δουλεύουμε
3. Push to remote
4. Merge back to develop (μέσω Pull Request style ή direct merge)
5. Delete feature branch

**Μαθαίνεις:** Τον βασικό κύκλο ζωής ενός feature branch.

---

### Σενάριο 2: Merge Conflict Resolution
**Πότε:** Phase 3-4 (Ordering Service)
**Τι κάνουμε:**
1. Δημιουργούμε 2 branches που τροποποιούν τον ίδιο κώδικα
   (π.χ. `feature/ordering-domain` και `bugfix/fix-shared-entity`)
2. Κάνουμε merge τον ένα στο develop
3. Προσπαθούμε να κάνουμε merge τον δεύτερο → CONFLICT
4. Λύνουμε το conflict μέσα από το Visual Studio
5. Complete merge

**Μαθαίνεις:** Πώς διαβάζεις conflicts, πώς τα λύνεις, πότε
επιλέγεις "take mine" vs "take theirs" vs manual merge.

---

### Σενάριο 3: Soft Reset
**Πότε:** Κατά τη διάρκεια coding
**Τι κάνουμε:**
1. Κάνεις 3 μικρά commits
2. Αποφασίζεις ότι θα ήταν καλύτερα ένα commit
3. Soft reset στο commit πριν τα 3
4. Τα changes παραμένουν staged
5. Κάνεις ένα καθαρό commit

**Μαθαίνεις:** Soft reset = "ξε-κάνε commits αλλά κράτα τις αλλαγές".
Ιδανικό για να "καθαρίσεις" το history πριν κάνεις push.

**Στο Visual Studio:** Right-click commit → Reset → Keep Changes (Soft)

---

### Σενάριο 4: Hard Reset (ΠΡΟΣΟΧΗ)
**Πότε:** Σκόπιμα δημιουργημένο σενάριο
**Τι κάνουμε:**
1. Κάνεις κάποιες αλλαγές και commits
2. Αποφασίζεις ότι όλα ήταν λάθος
3. Hard reset σε previous commit
4. ΟΛΑ χάνονται (uncommitted + commits)

**Μαθαίνεις:** Hard reset = "πέταξε τα ΟΛΛΑ". Επικίνδυνο αλλά
μερικές φορές απαραίτητο. Πρέπει να ξέρεις τι κάνει.

**Στο Visual Studio:** Right-click commit → Reset → Delete Changes (Hard)

**ΚΑΝΟΝΑΣ:** Ποτέ hard reset σε commits που έχεις ήδη push-αρει
σε shared branch. Μόνο σε τοπικά commits.

---

### Σενάριο 5: Revert Commit
**Πότε:** Phase 5-6
**Τι κάνουμε:**
1. Κάνεις push ένα commit στο develop
2. Συνειδητοποιείς ότι είχε bug
3. Δεν μπορείς να κάνεις reset (είναι ήδη pushed)
4. Κάνεις revert → δημιουργείται ΝΕΟ commit που αναιρεί τις αλλαγές
5. Push το revert commit

**Μαθαίνεις:** Η διαφορά reset vs revert:
- Reset = σβήνει ιστορία (μόνο locally)
- Revert = δημιουργεί νέα ιστορία που αναιρεί (safe για shared branches)

**Στο Visual Studio:** Right-click commit → Revert

---

### Σενάριο 6: Cherry-Pick
**Πότε:** Phase 7-8
**Τι κάνουμε:**
1. Στο `feature/api-gateway` κάνεις ένα commit που fixes κάτι στο Shared project
2. Αυτό το fix χρειάζεται και στο develop ΤΩΡΑ (πριν γίνει merge όλο το feature)
3. Switch στο develop
4. Cherry-pick ΜΟΝΟ εκείνο το commit
5. Τώρα το develop έχει μόνο το fix, χωρίς τα υπόλοιπα αλλαγές

**Μαθαίνεις:** Cherry-pick = "πάρε ένα συγκεκριμένο commit
και εφάρμοσέ το σε άλλο branch". Πολύ χρήσιμο σε emergencies.

**Στο Visual Studio:** Git Repository → βρες το commit → Right-click → Cherry-Pick

---

### Σενάριο 7: Stash
**Πότε:** Οποτεδήποτε κατά την υλοποίηση
**Τι κάνουμε:**
1. Δουλεύεις στο feature/ordering-api
2. Σε ζητάνε urgent να δεις κάτι στο develop
3. Έχεις uncommitted changes — δεν θέλεις να τα commit-αρεις μισοτελειωμένα
4. Git Stash → οι αλλαγές "κρύβονται"
5. Switch στο develop, κάνεις ό,τι χρειάζεται
6. Switch πίσω στο feature branch
7. Git Stash Pop → οι αλλαγές επανέρχονται

**Μαθαίνεις:** Stash = "κράτα τις αλλαγές μου στην άκρη προσωρινά".

**Στο Visual Studio:** Git Changes → "..." menu → Stash All

---

### Σενάριο 8: Release Branch & Tag
**Πότε:** Phase 9-10 (τελική φάση)
**Τι κάνουμε:**
1. Κόβουμε `release/v1.0` από develop
2. Κάνουμε τελικά fixes στο release branch
3. Merge release → main
4. Tag: `v1.0`
5. Merge release → develop (για να πάρει τα fixes)
6. Delete release branch

**Μαθαίνεις:** Πώς γίνεται ένα release σε enterprise environment.

---

### Σενάριο 9: Hotfix
**Πότε:** Μετά το release (σκόπιμα σενάριο)
**Τι κάνουμε:**
1. "Ανακαλύπτουμε" critical bug στο main
2. Κόβουμε `hotfix/critical-fix` ΑΠΟ main (όχι develop!)
3. Φτιάχνουμε το bug
4. Merge hotfix → main
5. Tag: `v1.0.1`
6. Merge hotfix → develop (ώστε να μην χαθεί το fix)

**Μαθαίνεις:** Hotfix flow — πώς αντιμετωπίζεις emergencies.

---

## Cheat Sheet: Reset vs Revert vs Checkout

| Εντολή | Τι κάνει | Αλλάζει history; | Safe σε shared branch; |
|--------|---------|------------------|----------------------|
| **Soft Reset** | Ξε-κάνει commits, κρατάει changes staged | Ναι | ΟΧΙ |
| **Mixed Reset** | Ξε-κάνει commits, κρατάει changes unstaged | Ναι | ΟΧΙ |
| **Hard Reset** | Ξε-κάνει commits ΚΑΙ σβήνει changes | Ναι | ΟΧΙ |
| **Revert** | Δημιουργεί νέο commit που αναιρεί | Όχι | ΝΑΙ |
| **Checkout** | Πηγαίνει σε branch/commit | Όχι | ΝΑΙ |
| **Cherry-pick** | Αντιγράφει commit σε τρέχον branch | Όχι (προσθέτει) | ΝΑΙ |
| **Stash** | Κρύβει uncommitted changes | Όχι | ΝΑΙ |

---

## Golden Rules

1. **Ποτέ force push σε shared branch** (main, develop)
2. **Commit νωρίς, commit συχνά** — μικρά, focused commits
3. **Πάντα pull πριν αρχίσεις δουλειά** σε shared branch
4. **Feature branch = 1 feature μόνο** — μην μπλέκεις πράγματα
5. **Γράψε σωστό commit message** — ο μελλοντικός εαυτός σου θα σε ευχαριστήσει
6. **Μην κάνεις commit secrets** (.env, connection strings, passwords)
7. **Review τα changes πριν commit** — Git Changes panel, δες τα diffs
