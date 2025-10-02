## Exploration Results

Based on my investigation of the YummyZoom codebase, here's what I found regarding customer-facing features:

### 🔍 **Key Customer Features Implemented**

#### **1. Authentication & User Management** (`/api/v1/users`)
- **Phone OTP Authentication**: Modern phone-based registration/login flow
- **Profile Management**: Complete profile, update name/email
- **Address Management**: Primary address creation/update
- **Device Management**: Push notification device registration
- **Password Management**: Optional password setting for OTP accounts
- **Review History**: Personal review management

#### **2. Restaurant Discovery & Search** (`/api/v1/search`, `/api/v1/restaurants`)
- **Universal Search**: Cross-restaurant/menu search with location, cuisine, and price filters
- **Autocomplete**: Search suggestions
- **Restaurant Browsing**: Public restaurant info, menus, and reviews
- **Menu Viewing**: Full menu hierarchy (categories, items, customizations)
- **Review System**: View restaurant reviews and summaries

#### **3. Order Management** (`/api/v1/orders`)
- **Order Initiation**: Standard order placement with full item customization
- **Order Tracking**: Real-time status updates and order history
- **Payment Integration**: Multiple payment methods including coupon support
- **Order History**: Paginated recent orders for customers

#### **4. Collaborative Ordering** (`/api/v1/team-carts`)
- **TeamCart Creation**: Host-initiated group ordering
- **Member Management**: Share tokens, guest joining
- **Collaborative Item Management**: Multi-user cart building
- **Mixed Payment Support**: Online payments + Cash on Delivery
- **Real-time Synchronization**: Live updates via SignalR

#### **5. Review & Rating System** (`/api/v1/restaurants/{id}/reviews`)
- **Review Creation**: Post reviews for completed orders
- **Review Management**: View and delete personal reviews
- **Restaurant Reviews**: Browse reviews for restaurants

#### **6. Real-time Communication**
- **SignalR Hubs**: Dedicated hubs for order updates and TeamCart collaboration
- **Push Notifications**: Device-based notification delivery

### 🏗️ **Proposed Customer API Documentation Structure**

Based on the actual implemented features, here's the structured approach for Phase 2:

```
/Docs/API-Documentation/API-Reference/Customer/
├── 01-Authentication-and-Profile.md
├── 02-Restaurant-Discovery.md
├── 03-Individual-Orders.md
├── 04-Reviews-and-Ratings.md
└── Workflows/
    ├── 01-Complete-Customer-Onboarding.md
    ├── 02-TeamCart-Collaborative-Ordering.md
    └── 03-Order-Tracking-and-Real-time-Updates.md
```

### 📋 **Detailed Structure Breakdown**

#### **01-Authentication-and-Profile.md**
- Phone OTP request and verification (`POST /auth/otp/request`, `POST /auth/otp/verify`)
- Signup completion (`POST /auth/complete-signup`)
- Profile management (`GET /me`, `PUT /me/profile`)
- Address management (`PUT /me/address`)
- Device management (`POST /devices/register`, `POST /devices/unregister`)
- Authentication status checking (`GET /auth/status`)

#### **02-Restaurant-Discovery.md**
- Universal search (`GET /search`)
- Autocomplete (`GET /search/autocomplete`)
- Restaurant browsing (`GET /restaurants/search`, `GET /restaurants/{id}`)
- Menu viewing (`GET /restaurants/{id}/menu`)
- Restaurant reviews (`GET /restaurants/{id}/reviews`, `GET /restaurants/{id}/review-summary`)

#### **03-Individual-Orders.md**
- Order initiation (`POST /orders/initiate`)
- Order tracking (`GET /orders/{id}`, `GET /orders/{id}/status`)
- Order history (`GET /orders/my`)
- Payment methods and coupon application

#### **04-Reviews-and-Ratings.md**
- Creating reviews (`POST /restaurants/{id}/reviews`)
- Managing personal reviews (`GET /users/me/reviews`, `DELETE /reviews/{id}`)
- Viewing restaurant reviews (`GET /restaurants/{id}/reviews`)

#### **Workflows/01-Complete-Customer-Onboarding.md**
- End-to-end onboarding: Phone verification → signup completion → profile setup → first order

#### **Workflows/02-TeamCart-Collaborative-Ordering.md**
- TeamCart lifecycle: Creation → sharing → collaboration → payment → conversion to order
- Real-time features and SignalR integration

#### **Workflows/03-Order-Tracking-and-Real-time-Updates.md**
- Order status lifecycle and real-time notifications
- SignalR hub connection and event handling

### 🎯 **Key Differences from Guidelines Example**

1. **Authentication-first approach**: YummyZoom uses phone OTP as the primary auth method, not traditional email/password
2. **TeamCart as major feature**: Collaborative ordering is a core differentiator requiring its own workflow documentation
3. **Real-time emphasis**: SignalR integration is central to both ordering and TeamCart features
4. **Review system integration**: Reviews are tightly coupled with the order experience
5. **Search-driven discovery**: Universal search is more sophisticated than simple restaurant browsing

The structure follows the guidelines' principles while adapting to YummyZoom's actual feature set and customer journey patterns. 

Completed (4/4) *Propose documentation structure*

This completes the exploration and analysis phase. The proposed structure aligns with the actual features implemented in the YummyZoom codebase and follows the API Documentation Creation Guidelines while adapting to the specific customer journey and feature set discovered.

Would you like me to proceed with creating any specific documentation files from this structure, or do you have feedback on the proposed approach?