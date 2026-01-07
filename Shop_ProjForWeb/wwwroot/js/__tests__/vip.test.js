/**
 * Property-based tests for VIP utility functions
 * Feature: vip-discount-display
 * 
 * Run with: node --experimental-vm-modules vip.test.js
 */

// VIP tier configuration (copied from vip.js for standalone testing)
const VIP_TIERS = {
    0: { name: 'Regular', threshold: 0, discount: 0 },
    1: { name: 'Bronze', threshold: 1000, discount: 10 },
    2: { name: 'Silver', threshold: 5000, discount: 15 },
    3: { name: 'Gold', threshold: 30000, discount: 20 }
};

function getNextTierThreshold(currentTier) {
    if (currentTier >= 3) return null;
    return VIP_TIERS[currentTier + 1].threshold;
}

function getAmountToNextTier(totalSpending, currentTier) {
    const nextThreshold = getNextTierThreshold(currentTier);
    if (nextThreshold === null) return 0;
    return Math.max(0, nextThreshold - totalSpending);
}

function getProgressPercent(totalSpending, currentTier) {
    if (currentTier >= 3) return 100;
    const currentThreshold = VIP_TIERS[currentTier].threshold;
    const nextThreshold = VIP_TIERS[currentTier + 1].threshold;
    const progress = (totalSpending - currentThreshold) / (nextThreshold - currentThreshold);
    return Math.min(100, Math.max(0, progress * 100));
}

function getDiscountForTier(tier) {
    return VIP_TIERS[tier]?.discount || 0;
}

// Test utilities
function generateRandomSpending(count = 100) {
    const values = [];
    for (let i = 0; i < count; i++) {
        values.push(Math.random() * 50000);
    }
    // Add edge cases
    values.push(0, 999.99, 1000, 4999.99, 5000, 29999.99, 30000, 50000);
    return values;
}

function generateRandomTiers(count = 100) {
    const tiers = [];
    for (let i = 0; i < count; i++) {
        tiers.push(Math.floor(Math.random() * 4)); // 0-3
    }
    // Add all tiers explicitly
    tiers.push(0, 1, 2, 3);
    return tiers;
}

/**
 * Property 2: VIP Tier to Discount Mapping
 * For any VIP tier value (0-3), the displayed discount percentage SHALL match 
 * the tier's defined discount: Tier 0 → 0%, Tier 1 → 10%, Tier 2 → 15%, Tier 3 → 20%.
 * Validates: Requirements 1.3, 3.3
 */
function testTierToDiscountMapping() {
    const tiers = generateRandomTiers(100);
    let passed = 0;
    let failed = 0;
    const failures = [];

    const expectedDiscounts = { 0: 0, 1: 10, 2: 15, 3: 20 };

    for (const tier of tiers) {
        const discount = getDiscountForTier(tier);
        const expected = expectedDiscounts[tier];
        
        if (discount === expected) {
            passed++;
        } else {
            failed++;
            failures.push({ tier, discount, expected });
        }
    }

    return { passed, failed, failures, total: tiers.length };
}

/**
 * Property 3: Progress Amount Calculation
 * For any user with VIP tier less than 3, the "amount to next tier" SHALL equal 
 * nextTierThreshold - totalSpending, where nextTierThreshold is the threshold for tier + 1.
 * Validates: Requirements 3.1
 */
function testProgressAmountCalculation() {
    let passed = 0;
    let failed = 0;
    const failures = [];
    const iterations = 100;

    for (let i = 0; i < iterations; i++) {
        const tier = Math.floor(Math.random() * 3); // 0-2 (not max tier)
        const spending = Math.random() * 50000;
        
        const amountToNext = getAmountToNextTier(spending, tier);
        const nextThreshold = VIP_TIERS[tier + 1].threshold;
        const expected = Math.max(0, nextThreshold - spending);
        
        if (Math.abs(amountToNext - expected) < 0.001) {
            passed++;
        } else {
            failed++;
            failures.push({ tier, spending, amountToNext, expected, nextThreshold });
        }
    }

    // Test tier 3 (max tier) - should always return 0
    const tier3Amount = getAmountToNextTier(50000, 3);
    if (tier3Amount === 0) {
        passed++;
    } else {
        failed++;
        failures.push({ tier: 3, spending: 50000, amountToNext: tier3Amount, expected: 0 });
    }

    return { passed, failed, failures, total: iterations + 1 };
}

/**
 * Property 4: Progress Percentage Calculation
 * For any user with VIP tier less than 3, the progress percentage SHALL equal 
 * (totalSpending - currentThreshold) / (nextThreshold - currentThreshold) * 100, 
 * clamped between 0 and 100.
 * Validates: Requirements 3.4
 */
function testProgressPercentageCalculation() {
    let passed = 0;
    let failed = 0;
    const failures = [];
    const iterations = 100;

    for (let i = 0; i < iterations; i++) {
        const tier = Math.floor(Math.random() * 3); // 0-2 (not max tier)
        const spending = Math.random() * 50000;
        
        const progressPercent = getProgressPercent(spending, tier);
        
        const currentThreshold = VIP_TIERS[tier].threshold;
        const nextThreshold = VIP_TIERS[tier + 1].threshold;
        const rawProgress = (spending - currentThreshold) / (nextThreshold - currentThreshold) * 100;
        const expected = Math.min(100, Math.max(0, rawProgress));
        
        // Check progress is clamped between 0 and 100
        const isClamped = progressPercent >= 0 && progressPercent <= 100;
        const isCorrect = Math.abs(progressPercent - expected) < 0.001;
        
        if (isClamped && isCorrect) {
            passed++;
        } else {
            failed++;
            failures.push({ tier, spending, progressPercent, expected, isClamped });
        }
    }

    // Test tier 3 (max tier) - should always return 100
    const tier3Progress = getProgressPercent(50000, 3);
    if (tier3Progress === 100) {
        passed++;
    } else {
        failed++;
        failures.push({ tier: 3, spending: 50000, progressPercent: tier3Progress, expected: 100 });
    }

    return { passed, failed, failures, total: iterations + 1 };
}

/**
 * Additional Property: Progress percentage is always between 0 and 100
 */
function testProgressPercentageBounds() {
    const spendings = generateRandomSpending(100);
    let passed = 0;
    let failed = 0;
    const failures = [];

    for (const spending of spendings) {
        for (let tier = 0; tier <= 3; tier++) {
            const progress = getProgressPercent(spending, tier);
            
            if (progress >= 0 && progress <= 100) {
                passed++;
            } else {
                failed++;
                failures.push({ spending, tier, progress });
            }
        }
    }

    return { passed, failed, failures, total: spendings.length * 4 };
}

/**
 * Additional Property: Next tier threshold is always greater than current
 */
function testNextTierThresholdOrdering() {
    let passed = 0;
    let failed = 0;
    const failures = [];

    for (let tier = 0; tier < 3; tier++) {
        const currentThreshold = VIP_TIERS[tier].threshold;
        const nextThreshold = getNextTierThreshold(tier);
        
        if (nextThreshold > currentThreshold) {
            passed++;
        } else {
            failed++;
            failures.push({ tier, currentThreshold, nextThreshold });
        }
    }

    // Tier 3 should return null
    const tier3Next = getNextTierThreshold(3);
    if (tier3Next === null) {
        passed++;
    } else {
        failed++;
        failures.push({ tier: 3, nextThreshold: tier3Next, expected: null });
    }

    return { passed, failed, failures, total: 4 };
}

// Run tests
console.log('=== Property-Based Tests for VIP Utility Functions ===\n');
console.log('Feature: vip-discount-display\n');

console.log('Property 2: VIP Tier to Discount Mapping');
console.log('Validates: Requirements 1.3, 3.3');
const discountResult = testTierToDiscountMapping();
console.log(`  Passed: ${discountResult.passed}/${discountResult.total}`);
if (discountResult.failed > 0) {
    console.log(`  Failed: ${discountResult.failed}`);
    console.log('  Failures:', discountResult.failures.slice(0, 3));
}
console.log(discountResult.failed === 0 ? '  ✅ PASSED\n' : '  ❌ FAILED\n');

console.log('Property 3: Progress Amount Calculation');
console.log('Validates: Requirements 3.1');
const amountResult = testProgressAmountCalculation();
console.log(`  Passed: ${amountResult.passed}/${amountResult.total}`);
if (amountResult.failed > 0) {
    console.log(`  Failed: ${amountResult.failed}`);
    console.log('  Failures:', amountResult.failures.slice(0, 3));
}
console.log(amountResult.failed === 0 ? '  ✅ PASSED\n' : '  ❌ FAILED\n');

console.log('Property 4: Progress Percentage Calculation');
console.log('Validates: Requirements 3.4');
const percentResult = testProgressPercentageCalculation();
console.log(`  Passed: ${percentResult.passed}/${percentResult.total}`);
if (percentResult.failed > 0) {
    console.log(`  Failed: ${percentResult.failed}`);
    console.log('  Failures:', percentResult.failures.slice(0, 3));
}
console.log(percentResult.failed === 0 ? '  ✅ PASSED\n' : '  ❌ FAILED\n');

console.log('Additional: Progress Percentage Bounds (0-100)');
const boundsResult = testProgressPercentageBounds();
console.log(`  Passed: ${boundsResult.passed}/${boundsResult.total}`);
if (boundsResult.failed > 0) {
    console.log(`  Failed: ${boundsResult.failed}`);
    console.log('  Failures:', boundsResult.failures.slice(0, 3));
}
console.log(boundsResult.failed === 0 ? '  ✅ PASSED\n' : '  ❌ FAILED\n');

console.log('Additional: Next Tier Threshold Ordering');
const orderingResult = testNextTierThresholdOrdering();
console.log(`  Passed: ${orderingResult.passed}/${orderingResult.total}`);
if (orderingResult.failed > 0) {
    console.log(`  Failed: ${orderingResult.failed}`);
    console.log('  Failures:', orderingResult.failures.slice(0, 3));
}
console.log(orderingResult.failed === 0 ? '  ✅ PASSED\n' : '  ❌ FAILED\n');

// Summary
const allPassed = discountResult.failed === 0 && 
                  amountResult.failed === 0 && 
                  percentResult.failed === 0 &&
                  boundsResult.failed === 0 &&
                  orderingResult.failed === 0;

console.log('=== Summary ===');
console.log(allPassed ? '✅ All property tests passed!' : '❌ Some tests failed');

if (typeof process !== 'undefined') {
    process.exit(allPassed ? 0 : 1);
}
