/**
 * Property-based tests for stock validation
 * Feature: admin-inventory-management, Property 3 & 4
 * Validates: Requirements 2.4, 3.4, 3.5
 * 
 * Run with: node stockValidation.test.js
 */

// Import validation functions (copied for standalone testing)
function validateStockAdjustment(currentStock, adjustment, operation = 'add') {
    if (typeof adjustment !== 'number' || isNaN(adjustment)) {
        return { valid: false, error: 'Adjustment must be a valid number' };
    }

    if (adjustment === 0) {
        return { valid: false, error: 'Adjustment cannot be zero' };
    }

    if (operation === 'add' && adjustment <= 0) {
        return { valid: false, error: 'Quantity to add must be a positive number' };
    }

    if (operation === 'remove' && adjustment <= 0) {
        return { valid: false, error: 'Quantity to remove must be a positive number' };
    }

    if (operation === 'remove' && adjustment > currentStock) {
        return { 
            valid: false, 
            error: `Cannot remove more than available stock (${currentStock} units)` 
        };
    }

    return { valid: true };
}

function validateQuickAdjustment(currentStock, direction) {
    if (direction === 'decrement' && currentStock <= 0) {
        return { valid: false, error: 'Cannot decrease stock below zero' };
    }
    return { valid: true };
}

function parseStockInput(input) {
    if (input === null || input === undefined || input.toString().trim() === '') {
        return { valid: false, error: 'Please enter a quantity' };
    }

    const value = parseInt(input, 10);
    
    if (isNaN(value)) {
        return { valid: false, error: 'Please enter a valid number' };
    }

    return { valid: true, value };
}

// Test utilities
function randomInt(min, max) {
    return Math.floor(Math.random() * (max - min + 1)) + min;
}

function randomPositiveInt(max = 1000) {
    return randomInt(1, max);
}

/**
 * Property 3: Non-Positive Adjustment Rejection
 * For any stock adjustment operation with adjustment value <= 0 for add operations,
 * or <= 0 for remove operations, the system shall reject the operation.
 * Validates: Requirements 2.4, 3.5
 */
function testNonPositiveAdjustmentRejection() {
    let passed = 0;
    let failed = 0;
    const failures = [];
    const iterations = 100;

    for (let i = 0; i < iterations; i++) {
        const currentStock = randomInt(0, 1000);
        
        // Test zero adjustment for add
        const zeroAddResult = validateStockAdjustment(currentStock, 0, 'add');
        if (!zeroAddResult.valid) {
            passed++;
        } else {
            failed++;
            failures.push({ test: 'zero add', currentStock, result: zeroAddResult });
        }

        // Test negative adjustment for add
        const negativeAdd = -randomPositiveInt();
        const negAddResult = validateStockAdjustment(currentStock, negativeAdd, 'add');
        if (!negAddResult.valid) {
            passed++;
        } else {
            failed++;
            failures.push({ test: 'negative add', currentStock, adjustment: negativeAdd, result: negAddResult });
        }

        // Test zero adjustment for remove
        const zeroRemoveResult = validateStockAdjustment(currentStock, 0, 'remove');
        if (!zeroRemoveResult.valid) {
            passed++;
        } else {
            failed++;
            failures.push({ test: 'zero remove', currentStock, result: zeroRemoveResult });
        }

        // Test negative adjustment for remove
        const negativeRemove = -randomPositiveInt();
        const negRemoveResult = validateStockAdjustment(currentStock, negativeRemove, 'remove');
        if (!negRemoveResult.valid) {
            passed++;
        } else {
            failed++;
            failures.push({ test: 'negative remove', currentStock, adjustment: negativeRemove, result: negRemoveResult });
        }
    }

    return { passed, failed, failures, total: iterations * 4 };
}

/**
 * Property 4: Insufficient Stock Rejection
 * For any product with current stock S and any removal amount R where R > S,
 * the system shall reject the operation.
 * Validates: Requirements 3.4, 4.4
 */
function testInsufficientStockRejection() {
    let passed = 0;
    let failed = 0;
    const failures = [];
    const iterations = 100;

    for (let i = 0; i < iterations; i++) {
        const currentStock = randomInt(0, 100);
        const excessRemoval = currentStock + randomPositiveInt(100);
        
        const result = validateStockAdjustment(currentStock, excessRemoval, 'remove');
        
        if (!result.valid && result.error.includes('Cannot remove more')) {
            passed++;
        } else {
            failed++;
            failures.push({ currentStock, excessRemoval, result });
        }
    }

    return { passed, failed, failures, total: iterations };
}

/**
 * Property: Valid positive add adjustments are accepted
 * For any current stock and any positive adjustment, add operation should be valid
 */
function testValidAddAdjustments() {
    let passed = 0;
    let failed = 0;
    const failures = [];
    const iterations = 100;

    for (let i = 0; i < iterations; i++) {
        const currentStock = randomInt(0, 1000);
        const positiveAdjustment = randomPositiveInt();
        
        const result = validateStockAdjustment(currentStock, positiveAdjustment, 'add');
        
        if (result.valid) {
            passed++;
        } else {
            failed++;
            failures.push({ currentStock, positiveAdjustment, result });
        }
    }

    return { passed, failed, failures, total: iterations };
}

/**
 * Property: Valid remove adjustments (within stock) are accepted
 * For any current stock S > 0 and any removal R where 0 < R <= S, operation should be valid
 */
function testValidRemoveAdjustments() {
    let passed = 0;
    let failed = 0;
    const failures = [];
    const iterations = 100;

    for (let i = 0; i < iterations; i++) {
        const currentStock = randomPositiveInt(100);
        const validRemoval = randomInt(1, currentStock);
        
        const result = validateStockAdjustment(currentStock, validRemoval, 'remove');
        
        if (result.valid) {
            passed++;
        } else {
            failed++;
            failures.push({ currentStock, validRemoval, result });
        }
    }

    return { passed, failed, failures, total: iterations };
}

/**
 * Property: Quick decrement at zero stock is rejected
 */
function testQuickDecrementAtZero() {
    let passed = 0;
    let failed = 0;
    const failures = [];
    const iterations = 100;

    for (let i = 0; i < iterations; i++) {
        // Test with zero stock
        const result = validateQuickAdjustment(0, 'decrement');
        
        if (!result.valid) {
            passed++;
        } else {
            failed++;
            failures.push({ currentStock: 0, result });
        }
    }

    return { passed, failed, failures, total: iterations };
}

/**
 * Property: Quick increment is always valid
 */
function testQuickIncrementAlwaysValid() {
    let passed = 0;
    let failed = 0;
    const failures = [];
    const iterations = 100;

    for (let i = 0; i < iterations; i++) {
        const currentStock = randomInt(0, 1000);
        const result = validateQuickAdjustment(currentStock, 'increment');
        
        if (result.valid) {
            passed++;
        } else {
            failed++;
            failures.push({ currentStock, result });
        }
    }

    return { passed, failed, failures, total: iterations };
}

/**
 * Property: Non-numeric input is rejected
 */
function testNonNumericInputRejection() {
    const nonNumericInputs = ['abc', 'hello', '12abc', '', '   ', null, undefined, NaN, {}, []];
    let passed = 0;
    let failed = 0;
    const failures = [];

    for (const input of nonNumericInputs) {
        const result = parseStockInput(input);
        
        if (!result.valid) {
            passed++;
        } else {
            failed++;
            failures.push({ input, result });
        }
    }

    return { passed, failed, failures, total: nonNumericInputs.length };
}

/**
 * Property: Valid numeric input is parsed correctly
 */
function testValidNumericInputParsing() {
    let passed = 0;
    let failed = 0;
    const failures = [];
    const iterations = 100;

    for (let i = 0; i < iterations; i++) {
        const value = randomInt(-100, 1000);
        const input = value.toString();
        
        const result = parseStockInput(input);
        
        if (result.valid && result.value === value) {
            passed++;
        } else {
            failed++;
            failures.push({ input, expectedValue: value, result });
        }
    }

    return { passed, failed, failures, total: iterations };
}

// Run tests
console.log('=== Property-Based Tests for Stock Validation ===\n');
console.log('Feature: admin-inventory-management\n');

console.log('Property 3: Non-Positive Adjustment Rejection');
console.log('Validates: Requirements 2.4, 3.5');
const nonPosResult = testNonPositiveAdjustmentRejection();
console.log(`  Passed: ${nonPosResult.passed}/${nonPosResult.total}`);
if (nonPosResult.failed > 0) {
    console.log(`  Failed: ${nonPosResult.failed}`);
    console.log('  Failures:', nonPosResult.failures.slice(0, 2));
}
console.log(nonPosResult.failed === 0 ? '  ✅ PASSED\n' : '  ❌ FAILED\n');

console.log('Property 4: Insufficient Stock Rejection');
console.log('Validates: Requirements 3.4, 4.4');
const insuffResult = testInsufficientStockRejection();
console.log(`  Passed: ${insuffResult.passed}/${insuffResult.total}`);
if (insuffResult.failed > 0) {
    console.log(`  Failed: ${insuffResult.failed}`);
    console.log('  Failures:', insuffResult.failures.slice(0, 2));
}
console.log(insuffResult.failed === 0 ? '  ✅ PASSED\n' : '  ❌ FAILED\n');

console.log('Additional: Valid Add Adjustments Accepted');
const validAddResult = testValidAddAdjustments();
console.log(`  Passed: ${validAddResult.passed}/${validAddResult.total}`);
if (validAddResult.failed > 0) {
    console.log(`  Failed: ${validAddResult.failed}`);
    console.log('  Failures:', validAddResult.failures.slice(0, 2));
}
console.log(validAddResult.failed === 0 ? '  ✅ PASSED\n' : '  ❌ FAILED\n');

console.log('Additional: Valid Remove Adjustments Accepted');
const validRemoveResult = testValidRemoveAdjustments();
console.log(`  Passed: ${validRemoveResult.passed}/${validRemoveResult.total}`);
if (validRemoveResult.failed > 0) {
    console.log(`  Failed: ${validRemoveResult.failed}`);
    console.log('  Failures:', validRemoveResult.failures.slice(0, 2));
}
console.log(validRemoveResult.failed === 0 ? '  ✅ PASSED\n' : '  ❌ FAILED\n');

console.log('Additional: Quick Decrement at Zero Rejected');
const zeroDecResult = testQuickDecrementAtZero();
console.log(`  Passed: ${zeroDecResult.passed}/${zeroDecResult.total}`);
if (zeroDecResult.failed > 0) {
    console.log(`  Failed: ${zeroDecResult.failed}`);
    console.log('  Failures:', zeroDecResult.failures.slice(0, 2));
}
console.log(zeroDecResult.failed === 0 ? '  ✅ PASSED\n' : '  ❌ FAILED\n');

console.log('Additional: Quick Increment Always Valid');
const incResult = testQuickIncrementAlwaysValid();
console.log(`  Passed: ${incResult.passed}/${incResult.total}`);
if (incResult.failed > 0) {
    console.log(`  Failed: ${incResult.failed}`);
    console.log('  Failures:', incResult.failures.slice(0, 2));
}
console.log(incResult.failed === 0 ? '  ✅ PASSED\n' : '  ❌ FAILED\n');

console.log('Additional: Non-Numeric Input Rejected');
const nonNumResult = testNonNumericInputRejection();
console.log(`  Passed: ${nonNumResult.passed}/${nonNumResult.total}`);
if (nonNumResult.failed > 0) {
    console.log(`  Failed: ${nonNumResult.failed}`);
    console.log('  Failures:', nonNumResult.failures.slice(0, 2));
}
console.log(nonNumResult.failed === 0 ? '  ✅ PASSED\n' : '  ❌ FAILED\n');

console.log('Additional: Valid Numeric Input Parsed');
const numParseResult = testValidNumericInputParsing();
console.log(`  Passed: ${numParseResult.passed}/${numParseResult.total}`);
if (numParseResult.failed > 0) {
    console.log(`  Failed: ${numParseResult.failed}`);
    console.log('  Failures:', numParseResult.failures.slice(0, 2));
}
console.log(numParseResult.failed === 0 ? '  ✅ PASSED\n' : '  ❌ FAILED\n');

// Summary
const allPassed = nonPosResult.failed === 0 && 
                  insuffResult.failed === 0 && 
                  validAddResult.failed === 0 &&
                  validRemoveResult.failed === 0 &&
                  zeroDecResult.failed === 0 &&
                  incResult.failed === 0 &&
                  nonNumResult.failed === 0 &&
                  numParseResult.failed === 0;

console.log('=== Summary ===');
console.log(allPassed ? '✅ All property tests passed!' : '❌ Some tests failed');

if (typeof process !== 'undefined') {
    process.exit(allPassed ? 0 : 1);
}
