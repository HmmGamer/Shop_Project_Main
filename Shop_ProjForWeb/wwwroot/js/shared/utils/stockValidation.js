/**
 * Stock adjustment validation utility
 * Validates stock adjustments before sending to the API
 */

/**
 * Validates a stock adjustment operation
 * @param {number} currentStock - The current stock quantity
 * @param {number} adjustment - The adjustment amount (positive to add, negative to remove)
 * @param {string} operation - The operation type ('add' or 'remove')
 * @returns {{ valid: boolean, error?: string }} Validation result
 */
export function validateStockAdjustment(currentStock, adjustment, operation = 'add') {
    // Check if adjustment is a valid number
    if (typeof adjustment !== 'number' || isNaN(adjustment)) {
        return { valid: false, error: 'Adjustment must be a valid number' };
    }

    // Check if adjustment is zero
    if (adjustment === 0) {
        return { valid: false, error: 'Adjustment cannot be zero' };
    }

    // For add operations, adjustment must be positive
    if (operation === 'add' && adjustment <= 0) {
        return { valid: false, error: 'Quantity to add must be a positive number' };
    }

    // For remove operations, adjustment must be positive (we'll negate it internally)
    if (operation === 'remove' && adjustment <= 0) {
        return { valid: false, error: 'Quantity to remove must be a positive number' };
    }

    // Check if removal would result in negative stock
    if (operation === 'remove' && adjustment > currentStock) {
        return { 
            valid: false, 
            error: `Cannot remove more than available stock (${currentStock} units)` 
        };
    }

    return { valid: true };
}

/**
 * Validates a quick adjustment (increment/decrement by 1)
 * @param {number} currentStock - The current stock quantity
 * @param {string} direction - 'increment' or 'decrement'
 * @returns {{ valid: boolean, error?: string }} Validation result
 */
export function validateQuickAdjustment(currentStock, direction) {
    if (direction === 'decrement' && currentStock <= 0) {
        return { valid: false, error: 'Cannot decrease stock below zero' };
    }
    return { valid: true };
}

/**
 * Parses and validates user input for stock adjustment
 * @param {string} input - The user input string
 * @returns {{ valid: boolean, value?: number, error?: string }} Parse result
 */
export function parseStockInput(input) {
    if (input === null || input === undefined || input.toString().trim() === '') {
        return { valid: false, error: 'Please enter a quantity' };
    }

    const value = parseInt(input, 10);
    
    if (isNaN(value)) {
        return { valid: false, error: 'Please enter a valid number' };
    }

    return { valid: true, value };
}
