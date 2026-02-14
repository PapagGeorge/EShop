# 05 - API Contracts

## Base URLs (via API Gateway)

| Service | Gateway Route | Actual Service URL |
|---------|--------------|-------------------|
| Identity | `http://localhost:5000/api/auth/*` | `http://identity-api:8080/api/auth/*` |
| Ordering | `http://localhost:5000/api/orders/*` | `http://ordering-api:8080/api/orders/*` |
| Catalog | `http://localhost:5000/api/products/*` | `http://catalog-api:8080/api/products/*` |

Ο client μιλάει ΜΟΝΟ στο Gateway (port 5000).
Δεν γνωρίζει ότι υπάρχουν πολλά services πίσω.

---

## Identity Service Endpoints

### POST /api/auth/register
**Auth:** Anonymous
**Request:**
```json
{
  "email": "giorgos@example.com",
  "password": "MyStr0ngP@ss",
  "fullName": "Giorgos Papageorgiou"
}
```
**Response 201:**
```json
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "email": "giorgos@example.com",
  "fullName": "Giorgos Papageorgiou"
}
```
**Response 400 (Validation):**
```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
  "title": "Validation Failed",
  "status": 400,
  "errors": {
    "email": ["Email is already registered."],
    "password": ["Password must be at least 8 characters."]
  }
}
```

---

### POST /api/auth/login
**Auth:** Anonymous
**Request:**
```json
{
  "email": "giorgos@example.com",
  "password": "MyStr0ngP@ss"
}
```
**Response 200:**
```json
{
  "token": "eyJhbGciOiJIUzI1NiIs...",
  "expiresAt": "2024-01-15T11:30:00Z",
  "user": {
    "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "email": "giorgos@example.com",
    "fullName": "Giorgos Papageorgiou",
    "role": "User"
  }
}
```
**Response 401:**
```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
  "title": "Unauthorized",
  "status": 401,
  "detail": "Invalid email or password."
}
```

---

## Ordering Service Endpoints

### POST /api/orders
**Auth:** Bearer Token (Role: User, Admin)
**Request:**
```json
{
  "shippingAddress": {
    "street": "Ερμού 15",
    "city": "Αθήνα",
    "zipCode": "10563",
    "country": "Greece"
  },
  "items": [
    {
      "productId": "a1b2c3d4-...",
      "quantity": 2
    },
    {
      "productId": "e5f6g7h8-...",
      "quantity": 1
    }
  ]
}
```
**Σημείωση:** Ο client στέλνει μόνο productId & quantity.
Τα productName και unitPrice λαμβάνονται από τον Catalog Service
(ο client δεν τα ορίζει — αυτό αποτρέπει price manipulation).

**Response 201:**
```json
{
  "id": "f47ac10b-58cc-4372-a567-0e02b2c3d479",
  "userId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "status": "Pending",
  "shippingAddress": {
    "street": "Ερμού 15",
    "city": "Αθήνα",
    "zipCode": "10563",
    "country": "Greece"
  },
  "items": [
    {
      "productId": "a1b2c3d4-...",
      "productName": "Wireless Mouse",
      "unitPrice": 29.99,
      "quantity": 2,
      "totalPrice": 59.98
    },
    {
      "productId": "e5f6g7h8-...",
      "productName": "USB-C Cable",
      "unitPrice": 12.50,
      "quantity": 1,
      "totalPrice": 12.50
    }
  ],
  "totalAmount": 72.48,
  "createdAt": "2024-01-15T10:30:00Z"
}
```
**Response 400 (Validation):**
```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
  "title": "Validation Failed",
  "status": 400,
  "errors": {
    "items": ["At least one item is required."],
    "items[0].quantity": ["Quantity must be greater than 0."]
  }
}
```
**Response 422 (Business Rule):**
```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
  "title": "Business Rule Violation",
  "status": 422,
  "detail": "Product with id 'a1b2c3d4-...' was not found in catalog."
}
```

---

### GET /api/orders/{id}
**Auth:** Bearer Token (Role: User, Admin)
**Response 200:**
```json
{
  "id": "f47ac10b-...",
  "userId": "3fa85f64-...",
  "status": "Pending",
  "shippingAddress": {
    "street": "Ερμού 15",
    "city": "Αθήνα",
    "zipCode": "10563",
    "country": "Greece"
  },
  "items": [
    {
      "productId": "a1b2c3d4-...",
      "productName": "Wireless Mouse",
      "unitPrice": 29.99,
      "quantity": 2,
      "totalPrice": 59.98
    }
  ],
  "totalAmount": 72.48,
  "createdAt": "2024-01-15T10:30:00Z",
  "updatedAt": null
}
```
**Response 404:**
```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.4",
  "title": "Not Found",
  "status": 404,
  "detail": "Order with id 'f47ac10b-...' was not found."
}
```

---

### PATCH /api/orders/{id}/cancel
**Auth:** Bearer Token (Role: User, Admin)
**Request Body:** None
**Response 200:**
```json
{
  "id": "f47ac10b-...",
  "status": "Cancelled",
  "updatedAt": "2024-01-15T11:00:00Z"
}
```
**Response 409 (Conflict — invalid state transition):**
```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.8",
  "title": "Conflict",
  "status": 409,
  "detail": "Order cannot be cancelled because its current status is 'Confirmed'. Only 'Pending' orders can be cancelled."
}
```

---

### GET /api/orders
**Auth:** Bearer Token (Role: User, Admin)
**Query Parameters:**
| Param | Type | Required | Default | Description |
|-------|------|----------|---------|-------------|
| status | string | No | - | Filter by status |
| page | int | No | 1 | Page number |
| pageSize | int | No | 10 | Items per page (max 50) |

**Response 200:**
```json
{
  "items": [
    {
      "id": "f47ac10b-...",
      "status": "Pending",
      "totalAmount": 72.48,
      "itemCount": 2,
      "createdAt": "2024-01-15T10:30:00Z"
    },
    {
      "id": "c89de21a-...",
      "status": "Confirmed",
      "totalAmount": 29.99,
      "itemCount": 1,
      "createdAt": "2024-01-14T15:00:00Z"
    }
  ],
  "page": 1,
  "pageSize": 10,
  "totalCount": 2,
  "totalPages": 1
}
```

---

## Catalog Service Endpoints (Minimal)

### GET /api/products
**Auth:** Anonymous (internal service)
**Response 200:**
```json
[
  {
    "id": "a1b2c3d4-1234-5678-9abc-def012345678",
    "name": "Wireless Mouse",
    "price": 29.99,
    "category": "Electronics"
  },
  {
    "id": "e5f6g7h8-1234-5678-9abc-def012345678",
    "name": "USB-C Cable",
    "price": 12.50,
    "category": "Accessories"
  },
  {
    "id": "i9j0k1l2-1234-5678-9abc-def012345678",
    "name": "Mechanical Keyboard",
    "price": 89.99,
    "category": "Electronics"
  }
]
```

### GET /api/products/{id}
**Response 200:** Single product object
**Response 404:** Product not found

### POST /api/products/batch
**Σκοπός:** Ανάκτηση πολλών products ταυτόχρονα (χρησιμοποιείται από Ordering Service)
**Request:**
```json
{
  "productIds": ["a1b2c3d4-...", "e5f6g7h8-..."]
}
```
**Response 200:**
```json
[
  {
    "id": "a1b2c3d4-...",
    "name": "Wireless Mouse",
    "price": 29.99
  },
  {
    "id": "e5f6g7h8-...",
    "name": "USB-C Cable",
    "price": 12.50
  }
]
```

---

## Common Headers

### Request Headers
| Header | Value | Required |
|--------|-------|----------|
| Authorization | Bearer {token} | Yes (εκτός auth endpoints) |
| Content-Type | application/json | Yes (POST/PUT/PATCH) |
| X-Correlation-Id | Guid | Optional (auto-generated αν λείπει) |

### Response Headers
| Header | Value | Description |
|--------|-------|-------------|
| X-Correlation-Id | Guid | Για tracing across services |
| X-RateLimit-Remaining | int | Εναπομείναντα requests |

---

## HTTP Status Codes Convention

| Code | Σημασία | Πότε χρησιμοποιείται |
|------|---------|---------------------|
| 200 | OK | Successful GET, PATCH |
| 201 | Created | Successful POST (resource created) |
| 400 | Bad Request | Validation errors (input format) |
| 401 | Unauthorized | Missing or invalid token |
| 403 | Forbidden | Valid token αλλά insufficient permissions |
| 404 | Not Found | Resource δεν βρέθηκε |
| 409 | Conflict | Invalid state transition |
| 422 | Unprocessable Entity | Business rule violation |
| 429 | Too Many Requests | Rate limit exceeded |
| 500 | Internal Server Error | Unexpected error |
