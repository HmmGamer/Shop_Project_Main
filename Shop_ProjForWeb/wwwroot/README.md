# Shop Frontend

A simple, responsive frontend for the Shop_ProjForWeb e-commerce system built with vanilla HTML, CSS, and JavaScript.

## Features

### Customer Features
- **Product Catalog**: Browse products with search, filtering, and pagination
- **Shopping Cart**: Add/remove items, adjust quantities, view totals
- **User Accounts**: Simple registration and login system
- **Order Management**: Place orders, view order history, track status
- **VIP Benefits**: Automatic 5% discount for VIP members

### Admin Features
- **Product Management**: Create, edit, delete products with image upload
- **Inventory Management**: Track stock levels, update quantities, low stock alerts
- **Order Processing**: View all orders, process payments, manage order status

## Getting Started

1. **Start the Backend**: Run the ASP.NET Core application
   ```bash
   cd Shop_ProjForWeb
   dotnet run
   ```

2. **Access the Frontend**: Open your browser and navigate to:
   ```
   http://localhost:5227
   ```

3. **Test Data**: The application includes sample users and products for testing

## Usage Guide

### For Customers

1. **Browse Products**: 
   - Visit the catalog page to see all available products
   - Use the search bar to find specific items
   - Sort by name, price, or date added

2. **Create Account**:
   - Click "Account" in the navigation
   - Switch to "Register" tab
   - Enter your full name (phone number is optional)

3. **Shopping**:
   - Add products to cart from the catalog
   - View cart to adjust quantities or remove items
   - Proceed to checkout (requires login)

4. **VIP Status**:
   - VIP members get automatic 5% discount on all orders
   - VIP status is based on purchase history

### For Administrators

1. **Enable Admin Mode**:
   - Create/login to an account
   - Go to Account > Profile
   - Toggle "Enable Admin Mode (Demo)"

2. **Manage Products**:
   - Navigate to Admin > Products
   - Add new products with images
   - Edit existing products
   - Activate/deactivate products

3. **Monitor Inventory**:
   - Navigate to Admin > Inventory
   - View stock levels for all products
   - Update quantities as needed
   - Monitor low stock alerts

4. **Process Orders**:
   - Navigate to Admin > Orders
   - View all customer orders
   - Process payments for pending orders
   - View detailed order information

## Sample Test Users

The application includes pre-seeded test users. Some examples:

- **Regular Users**: John Hooper, Michael Rodriguez, David Thompson
- **VIP Users**: Sarah Mitchell, Emily Johnson, Jessica Williams
- **High Spenders**: Robert Wilson, Andrew Robinson, Christopher Brown

You can login using any full name from the seeded data, or create new accounts.

## Technical Features

### Architecture
- **Frontend**: Vanilla JavaScript with modular design
- **State Management**: Local storage with automatic sync
- **API Communication**: RESTful API client with error handling
- **Responsive Design**: Mobile-first CSS with flexbox/grid

### Key Components
- **Router**: Client-side routing for SPA experience
- **State Manager**: Centralized state with event system
- **API Client**: HTTP client with retry logic and error handling
- **UI Components**: Reusable components for consistent design

### Error Handling
- Network error detection with retry options
- Form validation with immediate feedback
- Graceful degradation for offline scenarios
- User-friendly error messages

### Performance Features
- Data caching with automatic refresh
- Lazy loading for large datasets
- Debounced search for better UX
- Optimistic UI updates

## Browser Support

- Chrome 80+
- Firefox 75+
- Safari 13+
- Edge 80+

## Development Notes

This is a demonstration frontend built for educational purposes. In a production environment, you would typically:

- Use a modern framework (React, Vue, Angular)
- Implement proper authentication with JWT tokens
- Add comprehensive testing (unit, integration, e2e)
- Use a build system (Webpack, Vite) for optimization
- Implement proper security measures (CSP, HTTPS)
- Add accessibility features (ARIA, keyboard navigation)
- Use a CSS framework or design system

## File Structure

```
wwwroot/
├── index.html              # Main HTML template
├── css/
│   └── styles.css          # Complete CSS framework
├── js/
│   ├── app.js              # Main application controller
│   ├── api-client.js       # API communication layer
│   ├── state-manager.js    # State management
│   ├── router.js           # Client-side routing
│   ├── components.js       # Reusable UI components
│   └── pages/
│       ├── catalog.js      # Product catalog page
│       ├── cart.js         # Shopping cart page
│       ├── account.js      # User account page
│       └── admin.js        # Admin dashboard
└── images/
    └── placeholder-product.svg  # Default product image
```

## API Endpoints Used

- `GET /api/products` - Get products with pagination
- `GET /api/products/search` - Search products
- `POST /api/products` - Create product (admin)
- `PUT /api/products/{id}` - Update product (admin)
- `DELETE /api/products/{id}` - Delete product (admin)
- `POST /api/products/{id}/image` - Upload product image (admin)
- `GET /api/users` - Get users
- `POST /api/users` - Create user account
- `PUT /api/users/{id}` - Update user profile
- `GET /api/orders` - Get orders
- `GET /api/orders/user/{userId}` - Get user orders
- `POST /api/orders` - Create order
- `POST /api/orders/{id}/pay` - Process payment (admin)
- `DELETE /api/orders/{id}` - Cancel order
- `GET /api/inventory` - Get inventory (admin)
- `PUT /api/inventory/{productId}` - Update inventory (admin)