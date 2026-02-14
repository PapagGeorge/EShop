# 01 - Business Requirements

## Project: E-Commerce Order Management System

### Overview
Microservice-based σύστημα διαχείρισης παραγγελιών για e-commerce platform.
Στόχος: enterprise-grade implementation με focus στο Ordering bounded context.

---

## Bounded Contexts (Services)

| Service | Ρόλος | Scope |
|---------|-------|-------|
| **Ordering Service** | Δημιουργία, διαχείριση, ακύρωση παραγγελιών | Full implementation |
| **Catalog Service** | Προϊόντα, τιμές | Minimal — dummy data, service-to-service communication demo |
| **Identity Service** | Register, Login, JWT generation | Minimal — 2 endpoints |
| **API Gateway** | Routing, Rate Limiting, Auth validation | YARP |
| **Payment** | Πληρωμές | Mock — δεν υλοποιείται |

---

## Use Cases

### UC-1: Register User (Identity Service)
**Actor:** Anonymous User
**Flow:**
1. POST /api/auth/register με email, password, fullName
2. Validate input (email format, password strength)
3. Hash password (BCrypt)
4. Αποθήκευση user στη βάση
5. Επιστροφή 201 Created

**Business Rules:**
- Email πρέπει να είναι unique
- Password minimum 8 χαρακτήρες, 1 uppercase, 1 number
- Ο χρήστης δημιουργείται με Role = "User"

---

### UC-2: Login User (Identity Service)
**Actor:** Registered User
**Flow:**
1. POST /api/auth/login με email, password
2. Ανάκτηση user από τη βάση
3. Verify password hash
4. Generate JWT token με claims (userId, email, role)
5. Επιστροφή 200 OK με token

**Business Rules:**
- Token expiration: 1 ώρα
- Claims: sub (userId), email, role
- Signing: symmetric key (HMAC SHA256) — σε production θα ήταν asymmetric

---

### UC-3: Create Order (Ordering Service)
**Actor:** Authenticated User
**Preconditions:** Valid JWT token
**Flow:**
1. POST /api/orders με items (productId, quantity)
2. Το σύστημα καλεί τον Catalog Service για validate products & τιμές
3. Δημιουργία Order aggregate με status = Pending
4. Υπολογισμός TotalAmount (sum of quantity * unitPrice per item)
5. Αποθήκευση στη βάση
6. Publish domain event: OrderCreated στο RabbitMQ
7. Επιστροφή 201 Created με OrderId

**Business Rules:**
- Minimum 1 item ανά order
- Quantity > 0 για κάθε item
- Όλα τα products πρέπει να υπάρχουν στον Catalog
- Ο UserId λαμβάνεται από το JWT token (ο χρήστης δεν τον στέλνει)
- Δεν επιτρέπονται duplicate productIds στο ίδιο order

---

### UC-4: Get Order by Id (Ordering Service)
**Actor:** Authenticated User
**Flow:**
1. GET /api/orders/{id}
2. Ανάκτηση order από τη βάση
3. Επιστροφή order details με items

**Business Rules:**
- Ο χρήστης βλέπει μόνο τα δικά του orders
- Admin μπορεί να δει οποιοδήποτε order
- 404 αν δεν βρεθεί ή δεν ανήκει στον χρήστη

---

### UC-5: Cancel Order (Ordering Service)
**Actor:** Authenticated User
**Flow:**
1. PATCH /api/orders/{id}/cancel
2. Validate ότι το order ανήκει στον χρήστη
3. Validate ότι status = Pending
4. Αλλαγή status σε Cancelled
5. Publish domain event: OrderCancelled στο RabbitMQ
6. Επιστροφή 200 OK

**Business Rules:**
- Μόνο orders σε status Pending μπορούν να ακυρωθούν
- Admin μπορεί να ακυρώσει οποιοδήποτε Pending order

---

### UC-6: Get User Orders (Ordering Service)
**Actor:** Authenticated User
**Flow:**
1. GET /api/orders?status=Pending&page=1&pageSize=10
2. Ανάκτηση orders του authenticated user
3. Optional filtering by status
4. Pagination
5. Επιστροφή paginated λίστας

---

## Order Lifecycle (State Machine)

```
                    ┌───────────┐
         ┌─────────│  Pending   │──────────┐
         │         └───────────┘           │
         │              │                  │
    (user cancels)  (payment confirmed)    │
         │              │                  │
         v              v                  │
   ┌───────────┐  ┌───────────┐           │
   │ Cancelled │  │ Confirmed │           │
   └───────────┘  └───────────┘           │
                       │                   │
                  (admin ships)            │
                       │                   │
                       v                   │
                 ┌───────────┐             │
                 │  Shipped  │             │
                 └───────────┘             │
                       │                   │
                  (delivered)              │
                       │                   │
                       v                   │
                 ┌───────────┐             │
                 │ Delivered │             │
                 └───────────┘             │

```

**Valid Transitions:**
- Pending → Confirmed (μετά payment confirmation — mock)
- Pending → Cancelled (από τον χρήστη ή admin)
- Confirmed → Shipped (από admin/system)
- Shipped → Delivered (από admin/system)

**Invalid Transitions (domain rules):**
- Cancelled → οτιδήποτε
- Delivered → οτιδήποτε
- Confirmed → Cancelled (θα χρειαζόταν refund — out of scope)

---

## Non-Functional Requirements

| Requirement | Target |
|-------------|--------|
| Authentication | JWT Bearer tokens |
| Authorization | Role-based (User, Admin) |
| Logging | Centralized — Serilog + Seq |
| Resilience | Retry policies για HTTP calls (Polly) |
| API Response Time | < 500ms για queries |
| Rate Limiting | 100 requests/minute ανά client (στο Gateway) |
| Health Checks | Κάθε service εκθέτει /health endpoint |
| Containerization | Όλα τρέχουν σε Docker via docker-compose |
