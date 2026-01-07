import { BaseRepository } from './BaseRepository.js';
import { API } from '../../shared/constants/api.js';

/**
 * Product repository for product data access
 */
export class ProductRepository extends BaseRepository {
    constructor(apiService) {
        super(apiService, API.PRODUCTS);
    }

    async getAll(params = {}) {
        const defaultParams = {
            page: 1,
            pageSize: 12,
            sortBy: 'name',
            sortDescending: false,
            ...params
        };
        return this.api.get(this.endpoint, defaultParams);
    }

    async getActive() {
        return this.api.get(API.PRODUCTS_ACTIVE);
    }

    async search(name) {
        return this.api.get(API.PRODUCTS_SEARCH, { name });
    }

    async uploadImage(id, file) {
        const formData = new FormData();
        formData.append('file', file);
        return this.api.postFormData(API.PRODUCT_IMAGE(id), formData);
    }
}
