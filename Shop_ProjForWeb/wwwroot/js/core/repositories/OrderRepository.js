import { BaseRepository } from './BaseRepository.js';
import { API } from '../../shared/constants/api.js';

/**
 * Order repository for order data access
 */
export class OrderRepository extends BaseRepository {
    constructor(apiService) {
        super(apiService, API.ORDERS);
    }

    async getAll(params = {}) {
        const defaultParams = {
            page: 1,
            pageSize: 20,
            sortBy: 'orderId',
            sortDescending: true,
            ...params
        };
        return this.api.get(this.endpoint, defaultParams);
    }

    async getByUser(userId) {
        return this.api.get(API.ORDERS_BY_USER(userId));
    }

    async getByStatus(status) {
        return this.api.get(API.ORDERS_BY_STATUS(status));
    }

    async pay(orderId) {
        return this.api.post(API.ORDER_PAY(orderId));
    }

    async cancel(orderId) {
        return this.api.delete(API.ORDER_BY_ID(orderId));
    }
}
