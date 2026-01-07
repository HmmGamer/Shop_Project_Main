/**
 * Abstract base class for data repositories
 * Implements common CRUD operations
 */
export class BaseRepository {
    constructor(apiService, endpoint) {
        this.api = apiService;
        this.endpoint = endpoint;
    }

    /**
     * Get all items with pagination
     * @param {Object} params - Query parameters
     * @returns {Promise<Object>} Paginated response
     */
    async getAll(params = {}) {
        const defaultParams = {
            page: 1,
            pageSize: 20,
            sortBy: 'id',
            sortDescending: false,
            ...params
        };
        return this.api.get(this.endpoint, defaultParams);
    }

    /**
     * Get single item by ID
     * @param {string} id - Item identifier
     * @returns {Promise<Object>} Item data
     */
    async getById(id) {
        return this.api.get(`${this.endpoint}/${id}`);
    }

    /**
     * Create new item
     * @param {Object} data - Item data
     * @returns {Promise<Object>} Created item
     */
    async create(data) {
        return this.api.post(this.endpoint, data);
    }

    /**
     * Update existing item
     * @param {string} id - Item identifier
     * @param {Object} data - Updated data
     * @returns {Promise<Object>} Updated item
     */
    async update(id, data) {
        return this.api.put(`${this.endpoint}/${id}`, data);
    }

    /**
     * Delete item
     * @param {string} id - Item identifier
     * @returns {Promise<void>}
     */
    async delete(id) {
        return this.api.delete(`${this.endpoint}/${id}`);
    }
}
