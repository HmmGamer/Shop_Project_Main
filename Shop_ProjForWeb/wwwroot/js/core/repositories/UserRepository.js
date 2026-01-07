import { BaseRepository } from './BaseRepository.js';
import { API } from '../../shared/constants/api.js';

/**
 * User repository for user data access
 */
export class UserRepository extends BaseRepository {
    constructor(apiService) {
        super(apiService, API.USERS);
    }

    async getAll(params = {}) {
        const defaultParams = {
            page: 1,
            pageSize: 20,
            sortBy: 'fullName',
            sortDescending: false,
            ...params
        };
        return this.api.get(this.endpoint, defaultParams);
    }
}
