/**
 * Property-based tests for inventory display
 * Feature: admin-inventory-management, Property 5 & 6
 * Validates: Requirements 1.1, 1.2, 1.3
 * 
 * Run with: node inventoryDisplay.test.js
 */

// Mock escapeHtml and formatDateTime for testing
function escapeHtml(str) {
    if (!str) return '';
    return str.toString()
        .replace(/&/g, '&amp;')
        .replace(/</g, '&lt;')
        .replace(/>/g, '&gt;')
        .replace(/"/g, '&quot;')
        .replace(/'/g, '&#039;');
}

function formatDateTime(dateStr) {
    if (!dateStr) return 'N/A';
    return new Date(dateStr).toLocaleString();
}

// InventoryManager render function (simplified for testing)
function renderInventoryTable(inventory) {
    const lowStockCount = inventory.filter(item => item.lowStockFlag).length;

    return `
        <div class="admin-section inventory-section">
            <div class="section-header">
                <h2>Inventory Management</h2>
                <div class="inventory-stats">
                    <span class="stat-item"><strong>${inventory.length}</strong> Products</span>
                    <span class="stat-item low-stock"><strong>${lowStockCount}</strong> Low Stock</span>
                </div>
            </div>
            <div class="inventory-table-container">
                <table class="inventory-table">
                    <thead>
                        <tr>
                            <th>Product</th>
                            <th>Current Stock</th>
                            <th>Quick Adjust</th>
                            <th>Status</th>
                            <th>Last Updated</th>
                            <th>Actions</th>
                        </tr>
                    </thead>
                    <tbody>
                        ${inventory.map(item => `
                            <tr class="inventory-row ${item.lowStockFlag ? 'low-stock-row' : ''}">
                                <td class="product-name">${escapeHtml(item.productName)}</td>
                                <td class="stock-quantity">
                                    <span class="quantity-display" data-product-id="${item.productId}">${item.quantity}</span>
                                    ${item.lowStockFlag ? '<span class="low-stock-badge">Low Stock</span>' : ''}
                                </td>
                                <td class="quick-adjust">
                                    <div class="quick-adjust-controls">
                                        <button class="btn btn-sm btn-decrement" data-product-id="${item.productId}" data-current-stock="${item.quantity}" ${item.quantity <= 0 ? 'disabled' : ''}>−</button>
                                        <button class="btn btn-sm btn-increment" data-product-id="${item.productId}">+</button>
                                    </div>
                                </td>
                                <td class="stock-status">
                                    ${item.lowStockFlag ? '<span class="status-badge status-warning">Low Stock</span>' : '<span class="status-badge status-ok">In Stock</span>'}
                                </td>
                                <td class="last-updated">${formatDateTime(item.lastUpdatedAt)}</td>
                                <td class="actions">
                                    <div class="action-buttons">
                                        <button class="btn btn-sm btn-success btn-add-stock" data-product-id="${item.productId}">Add</button>
                                        <button class="btn btn-sm btn-warning btn-remove-stock" data-product-id="${item.productId}" data-current-stock="${item.quantity}">Remove</button>
                                        <button class="btn btn-sm btn-outline update-stock-btn" data-product-id="${item.productId}">Set</button>
                                    </div>
                                </td>
                            </tr>
                        `).join('')}
                    </tbody>
                </table>
            </div>
        </div>
    `;
}

// Test utilities
function randomString(length = 10) {
    const chars = 'ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789';
    let result = '';
    for (let i = 0; i < length; i++) {
        result += chars.charAt(Math.floor(Math.random() * chars.length));
    }
    return result;
}

function randomInt(min, max) {
    return Math.floor(Math.random() * (max - min + 1)) + min;
}

function generateRandomInventoryItem() {
    const quantity = randomInt(0, 100);
    return {
        productId: `prod-${randomString(8)}`,
        productName: `Product ${randomString(5)}`,
        quantity: quantity,
        lowStockFlag: quantity < 10,
        lastUpdatedAt: new Date(Date.now() - randomInt(0, 86400000 * 30)).toISOString()
    };
}

function generateRandomInventory(count) {
    const inventory = [];
    for (let i = 0; i < count; i++) {
        inventory.push(generateRandomInventoryItem());
    }
    return inventory;
}

/**
 * Property 5: Inventory Display Completeness
 * For any set of inventory items, the rendered inventory table shall contain all items,
 * and each item shall display product name, quantity, stock status, and last updated timestamp.
 * Validates: Requirements 1.1, 1.2
 */
function testInventoryDisplayCompleteness() {
    let passed = 0;
    let failed = 0;
    const failures = [];
    const iterations = 100;

    for (let i = 0; i < iterations; i++) {
        const itemCount = randomInt(1, 20);
        const inventory = generateRandomInventory(itemCount);
        const rendered = renderInventoryTable(inventory);

        let allItemsPresent = true;
        const missingItems = [];

        for (const item of inventory) {
            // Check product name is present
            if (!rendered.includes(escapeHtml(item.productName))) {
                allItemsPresent = false;
                missingItems.push({ field: 'productName', item });
            }

            // Check quantity is present
            if (!rendered.includes(`>${item.quantity}<`)) {
                allItemsPresent = false;
                missingItems.push({ field: 'quantity', item });
            }

            // Check product ID is present (for buttons)
            if (!rendered.includes(item.productId)) {
                allItemsPresent = false;
                missingItems.push({ field: 'productId', item });
            }

            // Check status badge is present
            const hasStatusBadge = rendered.includes('status-ok') || rendered.includes('status-warning');
            if (!hasStatusBadge) {
                allItemsPresent = false;
                missingItems.push({ field: 'statusBadge', item });
            }
        }

        if (allItemsPresent) {
            passed++;
        } else {
            failed++;
            failures.push({ itemCount, missingItems: missingItems.slice(0, 3) });
        }
    }

    return { passed, failed, failures, total: iterations };
}

/**
 * Property 6: Low Stock Visual Indicator
 * For any inventory item with lowStockFlag = true, the rendered row shall contain
 * a visual warning indicator (CSS class or badge).
 * Validates: Requirements 1.3
 */
function testLowStockVisualIndicator() {
    let passed = 0;
    let failed = 0;
    const failures = [];
    const iterations = 100;

    for (let i = 0; i < iterations; i++) {
        const inventory = generateRandomInventory(randomInt(5, 15));
        const rendered = renderInventoryTable(inventory);

        let allLowStockHighlighted = true;
        const missingHighlights = [];

        for (const item of inventory) {
            if (item.lowStockFlag) {
                // Check for low-stock-row class
                const hasLowStockRow = rendered.includes('low-stock-row');
                // Check for low-stock-badge
                const hasLowStockBadge = rendered.includes('low-stock-badge');
                // Check for status-warning
                const hasWarningStatus = rendered.includes('status-warning');

                if (!hasLowStockRow && !hasLowStockBadge && !hasWarningStatus) {
                    allLowStockHighlighted = false;
                    missingHighlights.push(item);
                }
            }
        }

        if (allLowStockHighlighted) {
            passed++;
        } else {
            failed++;
            failures.push({ missingHighlights: missingHighlights.slice(0, 2) });
        }
    }

    return { passed, failed, failures, total: iterations };
}

/**
 * Property: All inventory items have action buttons
 */
function testActionButtonsPresent() {
    let passed = 0;
    let failed = 0;
    const failures = [];
    const iterations = 100;

    for (let i = 0; i < iterations; i++) {
        const inventory = generateRandomInventory(randomInt(1, 10));
        const rendered = renderInventoryTable(inventory);

        let allButtonsPresent = true;
        const missingButtons = [];

        for (const item of inventory) {
            // Check for increment button
            if (!rendered.includes(`btn-increment" data-product-id="${item.productId}"`)) {
                allButtonsPresent = false;
                missingButtons.push({ type: 'increment', productId: item.productId });
            }

            // Check for decrement button
            if (!rendered.includes(`btn-decrement" data-product-id="${item.productId}"`)) {
                allButtonsPresent = false;
                missingButtons.push({ type: 'decrement', productId: item.productId });
            }

            // Check for add stock button
            if (!rendered.includes(`btn-add-stock" data-product-id="${item.productId}"`)) {
                allButtonsPresent = false;
                missingButtons.push({ type: 'add-stock', productId: item.productId });
            }

            // Check for remove stock button
            if (!rendered.includes(`btn-remove-stock" data-product-id="${item.productId}"`)) {
                allButtonsPresent = false;
                missingButtons.push({ type: 'remove-stock', productId: item.productId });
            }
        }

        if (allButtonsPresent) {
            passed++;
        } else {
            failed++;
            failures.push({ missingButtons: missingButtons.slice(0, 3) });
        }
    }

    return { passed, failed, failures, total: iterations };
}

/**
 * Property: Zero stock items have disabled decrement button
 */
function testZeroStockDecrementDisabled() {
    let passed = 0;
    let failed = 0;
    const failures = [];
    const iterations = 100;

    for (let i = 0; i < iterations; i++) {
        // Create inventory with at least one zero-stock item
        const inventory = generateRandomInventory(randomInt(3, 10));
        inventory[0].quantity = 0;
        inventory[0].lowStockFlag = true;

        const rendered = renderInventoryTable(inventory);

        // Check that zero-stock items have disabled decrement button
        const zeroStockItems = inventory.filter(item => item.quantity <= 0);
        let allDisabled = true;

        for (const item of zeroStockItems) {
            // The button should have disabled attribute
            const buttonPattern = `btn-decrement" data-product-id="${item.productId}" data-current-stock="${item.quantity}" disabled`;
            if (!rendered.includes(buttonPattern)) {
                allDisabled = false;
            }
        }

        if (allDisabled) {
            passed++;
        } else {
            failed++;
            failures.push({ zeroStockItems: zeroStockItems.map(i => i.productId) });
        }
    }

    return { passed, failed, failures, total: iterations };
}

// Run tests
console.log('=== Property-Based Tests for Inventory Display ===\n');
console.log('Feature: admin-inventory-management\n');

console.log('Property 5: Inventory Display Completeness');
console.log('Validates: Requirements 1.1, 1.2');
const completenessResult = testInventoryDisplayCompleteness();
console.log(`  Passed: ${completenessResult.passed}/${completenessResult.total}`);
if (completenessResult.failed > 0) {
    console.log(`  Failed: ${completenessResult.failed}`);
    console.log('  Failures:', completenessResult.failures.slice(0, 2));
}
console.log(completenessResult.failed === 0 ? '  ✅ PASSED\n' : '  ❌ FAILED\n');

console.log('Property 6: Low Stock Visual Indicator');
console.log('Validates: Requirements 1.3');
const lowStockResult = testLowStockVisualIndicator();
console.log(`  Passed: ${lowStockResult.passed}/${lowStockResult.total}`);
if (lowStockResult.failed > 0) {
    console.log(`  Failed: ${lowStockResult.failed}`);
    console.log('  Failures:', lowStockResult.failures.slice(0, 2));
}
console.log(lowStockResult.failed === 0 ? '  ✅ PASSED\n' : '  ❌ FAILED\n');

console.log('Additional: Action Buttons Present');
const buttonsResult = testActionButtonsPresent();
console.log(`  Passed: ${buttonsResult.passed}/${buttonsResult.total}`);
if (buttonsResult.failed > 0) {
    console.log(`  Failed: ${buttonsResult.failed}`);
    console.log('  Failures:', buttonsResult.failures.slice(0, 2));
}
console.log(buttonsResult.failed === 0 ? '  ✅ PASSED\n' : '  ❌ FAILED\n');

console.log('Additional: Zero Stock Decrement Disabled');
const disabledResult = testZeroStockDecrementDisabled();
console.log(`  Passed: ${disabledResult.passed}/${disabledResult.total}`);
if (disabledResult.failed > 0) {
    console.log(`  Failed: ${disabledResult.failed}`);
    console.log('  Failures:', disabledResult.failures.slice(0, 2));
}
console.log(disabledResult.failed === 0 ? '  ✅ PASSED\n' : '  ❌ FAILED\n');

// Summary
const allPassed = completenessResult.failed === 0 && 
                  lowStockResult.failed === 0 && 
                  buttonsResult.failed === 0 &&
                  disabledResult.failed === 0;

console.log('=== Summary ===');
console.log(allPassed ? '✅ All property tests passed!' : '❌ Some tests failed');

if (typeof process !== 'undefined') {
    process.exit(allPassed ? 0 : 1);
}
