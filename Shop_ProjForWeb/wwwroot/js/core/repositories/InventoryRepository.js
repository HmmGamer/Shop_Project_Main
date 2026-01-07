import { BaseRepository } from './BaseRepository.js';
import { API } from '../../shared/constants/api.js';

/**
 * Inventory repository for inventory data access
 */
export class InventoryRepository extends BaseRepository {
    constructor(apiService) {
        super(apiService, API.INVENTORY);
    }

    async getLowStock() {
        return this.api.get(API.INVENTORY_LOW_STOCK);
    }

    async updateStock(productId, data) {
        return this.api.put(API.INVENTORY_BY_PRODUCT(productId), data);
    }

    async getByProduct(productId) {
        return this.api.get(API.INVENTORY_BY_PRODUCT(productId));
    }

    /**
     * Adjusts stock by a given amount (positive to add, negative to remove)
     * @param {string} productId - The product ID
     * @param {number} adjustment - The adjustment amount
     * @returns {Promise} The updated inventory
     * @throws {Error} With meaningful message if operation fails
     */
    async adjustStock(productId, adjustment) {
        // Get current stock first
        let current;
        try {
            current = await this.getByProduct(productId);
        } catch (error) {
            // Re-throw with context about what failed
            const message = this.getErrorMessage(error, 'fetch current stock');
            throw new Error(message);
        }

        const newQuantity = current.quantity + adjustment;
        
        if (newQuantity < 0) {
            throw new Error(`Cannot reduce stock below zero. Current: ${current.quantity}, Adjustment: ${adjustment}`);
        }

        try {
            return await this.updateStock(productId, { quantity: newQuantity });
        } catch (error) {
            const message = this.getErrorMessage(error, 'update stock');
            throw new Error(message);
        }
    }

    /**
     * Extracts a user-friendly error message from an API error
     * @param {Error} error - The error object
     * @param {string} operation - Description of the operation that failed
     * @returns {string} User-friendly error message
     */
    getErrorMessage(error, operation) {
        if (error.status === 401) {
            return 'Unauthorized: Admin authentication required to modify inventory';
        }
        if (error.status === 403) {
            return 'Forbidden: You do not have permission to modify inventory';
        }
        if (error.status === 404) {
            return 'Product inventory not found';
        }
        if (error.data?.error) {
            return error.data.error;
        }
        if (error.message) {
            return error.message;
        }
        return `Failed to ${operation}`;
    }
}
