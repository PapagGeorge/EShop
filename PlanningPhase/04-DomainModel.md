# 04 - Domain Model (DDD)

## DDD Concepts που εφαρμόζουμε

Πριν δούμε τον κώδικα, ας καταλάβουμε τα concepts:

### Aggregate
Ένα cluster από entities που αντιμετωπίζονται ως μία μονάδα.
Όλες οι αλλαγές γίνονται μέσω του **Aggregate Root**.
Εξωτερικός κώδικας δεν μπορεί να αγγίξει τα εσωτερικά entities απευθείας.

### Aggregate Root
Η "πόρτα" του aggregate. Μόνο αυτό εκτίθεται στον έξω κόσμο.
Στο δικό μας case: **Order** είναι το Aggregate Root.

### Entity
Αντικείμενο με ταυτότητα (Id). Δύο entities με ίδια data αλλά
διαφορετικό Id είναι διαφορετικά.

### Value Object
Αντικείμενο χωρίς ταυτότητα. Ορίζεται μόνο από τα values του.
Π.χ. Address("Αθήνα", "12345") == Address("Αθήνα", "12345").
Είναι immutable.

### Domain Event
Κάτι που συνέβη στο domain. Π.χ. "Μια παραγγελία δημιουργήθηκε".
Χρησιμοποιείται για side effects (publish to message bus, send email κλπ).

---

## Ordering Domain Model

### Aggregate: Order (Aggregate Root)

```
Order (Aggregate Root)
│
├── Properties:
│   ├── Id: Guid
│   ├── UserId: Guid
│   ├── Status: OrderStatus (Value Object / Enum)
│   ├── TotalAmount: decimal
│   ├── ShippingAddress: Address (Value Object)
│   ├── CreatedAt: DateTime
│   └── UpdatedAt: DateTime?
│
├── Collections:
│   └── Items: List<OrderItem> (Entity)
│
├── Behaviors (Methods):
│   ├── AddItem(productId, productName, unitPrice, quantity)
│   ├── Cancel()
│   ├── Confirm()
│   ├── Ship()
│   └── Deliver()
│
└── Domain Events:
    ├── OrderCreatedDomainEvent
    └── OrderCancelledDomainEvent
```

### Entity: OrderItem

```
OrderItem (Entity, εσωτερικό του Order aggregate)
│
├── Properties:
│   ├── Id: Guid
│   ├── ProductId: Guid
│   ├── ProductName: string
│   ├── UnitPrice: decimal
│   ├── Quantity: int
│   └── TotalPrice: decimal (computed: UnitPrice * Quantity)
│
└── Behaviors:
    └── (δημιουργείται μόνο μέσω Order.AddItem)
```

### Value Object: Address

```
Address (Value Object — immutable)
│
├── Street: string
├── City: string
├── ZipCode: string
└── Country: string
```

### Enum: OrderStatus

```
OrderStatus
├── Pending = 0
├── Confirmed = 1
├── Shipped = 2
├── Delivered = 3
└── Cancelled = 4
```

---

## Domain Rules (Invariants)

Αυτοί είναι οι κανόνες που το Domain ΠΑΝΤΑ εγγυάται.
Αν κάποιος κανόνας παραβιαστεί, πετάμε Domain Exception.

### Order Invariants
1. **Ένα order πρέπει να έχει τουλάχιστον 1 item**
   - Δεν μπορείς να δημιουργήσεις κενό order

2. **Τα status transitions πρέπει να ακολουθούν το state machine**
   - Cancel() πετάει exception αν status != Pending
   - Confirm() πετάει exception αν status != Pending
   - Ship() πετάει exception αν status != Confirmed
   - Deliver() πετάει exception αν status != Shipped

3. **TotalAmount υπολογίζεται αυτόματα**
   - Δεν μπορεί να οριστεί εξωτερικά
   - Sum of all items' TotalPrice

4. **UserId δεν αλλάζει μετά τη δημιουργία**

### OrderItem Invariants
1. **Quantity > 0**
2. **UnitPrice >= 0**
3. **ProductId δεν μπορεί να είναι empty Guid**

---

## Domain Events

### OrderCreatedDomainEvent
```
{
  "orderId": "guid",
  "userId": "guid",
  "totalAmount": 150.00,
  "itemCount": 3,
  "occurredAt": "2024-01-15T10:30:00Z"
}
```
**Πότε:** Αμέσως μετά την επιτυχή δημιουργία order
**Σκοπός:** Publish στο RabbitMQ, ενημέρωση external systems

### OrderCancelledDomainEvent
```
{
  "orderId": "guid",
  "userId": "guid",
  "reason": "Customer requested cancellation",
  "occurredAt": "2024-01-15T11:00:00Z"
}
```
**Πότε:** Αμέσως μετά την ακύρωση
**Σκοπός:** Publish στο RabbitMQ, (future) restore inventory

---

## Identity Domain Model (Minimal)

### Entity: User
```
User
├── Id: Guid
├── Email: string
├── PasswordHash: string
├── FullName: string
├── Role: string ("User" | "Admin")
├── CreatedAt: DateTime
└── IsActive: bool
```

Το Identity domain δεν χρειάζεται DDD complexity —
είναι ένα απλό CRUD service. Δεν έχει aggregates ή domain events.

---

## Σχεδιαστικές Αποφάσεις

### Γιατί τα behaviors είναι μέσα στο Entity (Rich Domain Model)
Στον πραγματικό κόσμο βλέπεις δύο approaches:
- **Anemic Domain Model:** Entities = data bags, logic σε services
- **Rich Domain Model:** Entities περιέχουν business logic

Εμείς πάμε με Rich Domain Model γιατί:
- Τα business rules ζουν κοντά στα data
- Αδύνατο να παραβιαστεί invariant (π.χ. δεν μπορείς να κάνεις
  order.Status = Cancelled χωρίς να περάσεις από Cancel())
- Testable: κάνεις unit test το entity χωρίς DB, HTTP, κλπ

### Γιατί Domain Events και όχι απευθείας publish
Τα domain events μαζεύονται στο entity κατά τη διάρκεια
της business operation. Μετά το save, ένα MediatR handler
τα παίρνει και τα κάνει publish στο RabbitMQ.

Αυτό εξασφαλίζει:
- Το domain δεν ξέρει τίποτα για RabbitMQ
- Τα events δημοσιεύονται ΜΟΝΟ αν το save πετύχει
- Εύκολο testing (ελέγχεις τι events δημιουργήθηκαν)

### Γιατί private setters
Τα properties θα έχουν `{ get; private set; }`.
Αλλαγές γίνονται ΜΟΝΟ μέσω methods.
Αυτό προστατεύει τα invariants.
