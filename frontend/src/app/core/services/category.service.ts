import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { Category, CreateCategoryRequest, UpdateCategoryRequest } from '../models/category.model';
import { PagedResult } from '../models/paged-result.model';

@Injectable({
  providedIn: 'root'
})
export class CategoryService {
  private http = inject(HttpClient);
  private apiUrl = `${environment.apiUrl}/categories`;

  getAll(page: number = 1, pageSize: number = 20): Observable<PagedResult<Category>> {
    const params = new HttpParams()
      .set('page', page.toString())
      .set('pageSize', pageSize.toString());

    return this.http.get<PagedResult<Category>>(this.apiUrl, { params });
  }

  getById(id: string): Observable<Category> {
    return this.http.get<Category>(`${this.apiUrl}/${id}`);
  }

  create(data: CreateCategoryRequest): Observable<string> {
    return this.http.post<string>(this.apiUrl, data);
  }

  update(id: string, data: UpdateCategoryRequest): Observable<void> {
    return this.http.put<void>(`${this.apiUrl}/${id}`, data);
  }

  delete(id: string): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${id}`);
  }
}
