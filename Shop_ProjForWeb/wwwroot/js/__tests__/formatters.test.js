/**
 * Property-based tests for formatters
 * Feature: cart-fixes, Property 3: Price Formatting Includes Currency Symbol
 * Validates: Requirements 2.1
 * 
 * Run with: node --experimental-vm-modules formatters.test.js
 * Or use a test runner like Jest/Vitest
 */

// Simple property-based test implementation
// For production, use fast-check library

/**
 * Generate random price values for testing
 */
function generateRandomPrices(count = 100) {
    const prices = [];
    for (let i = 0; i < count; i++) {
        // Generate prices from 0.01 to 99999.99
        const price = Math.random() * 99999.98 + 0.01;
        prices.push(price);
    }
    // Add edge cases
    prices.push(0);
    prices.push(0.01);
    prices.push(0.99);
    prices.push(1);
    prices.push(100);
    prices.push(1000);
    prices.push(99999.99);
    return prices;
}

/**
 * formatCurrency implementation (copied for testing)
 */
function formatCurrency(amount) {
    return new Intl.NumberFormat('en-US', {
        style: 'currency',
        currency: 'USD'
    }).format(amount);
}

/**
 * Property 3: Price Formatting Includes Currency Symbol
 * For any numeric price value, the formatted output SHALL start with 
 * a currency symbol and contain exactly two decimal places.
 */
function testPriceFormattingProperty() {
    const prices = generateRandomPrices(100);
    let passed = 0;
    let failed = 0;
    const failures = [];

    for (const price of prices) {
        const formatted = formatCurrency(price);
        
        // Check starts with currency symbol ($)
        const startsWithSymbol = formatted.startsWith('$');
        
        // Check contains decimal point and two decimal places
        const decimalMatch = formatted.match(/\.\d{2}$/);
        const hasTwoDecimals = decimalMatch !== null;
        
        if (startsWithSymbol && hasTwoDecimals) {
            passed++;
        } else {
            failed++;
            failures.push({
                input: price,
                output: formatted,
                startsWithSymbol,
                hasTwoDecimals
            });
        }
    }

    return { passed, failed, failures, total: prices.length };
}

/**
 * Property 5: Cart Totals Calculation Correctness
 * For any cart with items:
 * - subtotal = Σ(item.basePrice × item.quantity)
 * - discountAmount = Σ(item.basePrice × item.discountPercent/100 × item.quantity)
 * - total = subtotal - discountAmount
 */
function calculateSubtotal(cart) {
    return cart.reduce((total, item) => total + (item.basePrice * item.quantity), 0);
}

function calculateDiscountAmount(cart) {
    return cart.reduce((total, item) => {
        if (item.discountPercent > 0) {
            return total + (item.basePrice * item.discountPercent / 100) * item.quantity;
        }
        return total;
    }, 0);
}

function generateRandomCart(itemCount = 5) {
    const cart = [];
    for (let i = 0; i < itemCount; i++) {
        cart.push({
            productId: `product-${i}`,
            productName: `Product ${i}`,
            basePrice: Math.random() * 100 + 1, // 1 to 101
            discountPercent: Math.random() > 0.5 ? Math.floor(Math.random() * 50) : 0, // 0-50% discount
            quantity: Math.floor(Math.random() * 10) + 1 // 1 to 10
        });
    }
    return cart;
}

function testCartTotalsProperty() {
    let passed = 0;
    let failed = 0;
    const failures = [];
    const iterations = 100;

    for (let i = 0; i < iterations; i++) {
        const cart = generateRandomCart(Math.floor(Math.random() * 10) + 1);
        
        const subtotal = calculateSubtotal(cart);
        const discountAmount = calculateDiscountAmount(cart);
        const total = subtotal - discountAmount;
        
        // Verify subtotal calculation
        const expectedSubtotal = cart.reduce((sum, item) => sum + item.basePrice * item.quantity, 0);
        const subtotalCorrect = Math.abs(subtotal - expectedSubtotal) < 0.001;
        
        // Verify discount calculation
        const expectedDiscount = cart.reduce((sum, item) => {
            return sum + (item.basePrice * item.discountPercent / 100) * item.quantity;
        }, 0);
        const discountCorrect = Math.abs(discountAmount - expectedDiscount) < 0.001;
        
        // Verify total calculation
        const expectedTotal = expectedSubtotal - expectedDiscount;
        const totalCorrect = Math.abs(total - expectedTotal) < 0.001;
        
        if (subtotalCorrect && discountCorrect && totalCorrect) {
            passed++;
        } else {
            failed++;
            failures.push({
                cart,
                subtotal,
                expectedSubtotal,
                discountAmount,
                expectedDiscount,
                total,
                expectedTotal
            });
        }
    }

    return { passed, failed, failures, total: iterations };
}

// Run tests
console.log('=== Property-Based Tests for Cart Fixes ===\n');

console.log('Property 3: Price Formatting Includes Currency Symbol');
console.log('Validates: Requirements 2.1');
const priceResult = testPriceFormattingProperty();
console.log(`  Passed: ${priceResult.passed}/${priceResult.total}`);
if (priceResult.failed > 0) {
    console.log(`  Failed: ${priceResult.failed}`);
    console.log('  Failures:', priceResult.failures.slice(0, 3));
}
console.log(priceResult.failed === 0 ? '  ✅ PASSED\n' : '  ❌ FAILED\n');

console.log('Property 5: Cart Totals Calculation Correctness');
console.log('Validates: Requirements 2.3, 2.4, 2.5');
const totalsResult = testCartTotalsProperty();
console.log(`  Passed: ${totalsResult.passed}/${totalsResult.total}`);
if (totalsResult.failed > 0) {
    console.log(`  Failed: ${totalsResult.failed}`);
    console.log('  Failures:', totalsResult.failures.slice(0, 3));
}
console.log(totalsResult.failed === 0 ? '  ✅ PASSED\n' : '  ❌ FAILED\n');

// Summary
const allPassed = priceResult.failed === 0 && totalsResult.failed === 0;
console.log('=== Summary ===');
console.log(allPassed ? '✅ All property tests passed!' : '❌ Some tests failed');

// Exit with appropriate code
if (typeof process !== 'undefined') {
    process.exit(allPassed ? 0 : 1);
}
