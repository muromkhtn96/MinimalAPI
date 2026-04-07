import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { Product, CreateProductRequest, UpdateProductRequest } from '../models/product.model';
import { PagedResult } from '../models/paged-result.model';

@Injectable({
  providedIn: 'root'
})
export class ProductService {
  private http = inject(HttpClient);
  private apiUrl = `${environment.apiUrl}/products`;

  getAll(page: number = 1, pageSize: number = 10, search?: string): Observable<PagedResult<Product>> {
    let params = new HttpParams()
      .set('page', page.toString())
      .set('pageSize', pageSize.toString());

    if (search) {
      params = params.set('search', search);
    }

    return this.http.get<PagedResult<Product>>(this.apiUrl, { params });
  }

  getById(id: string): Observable<Product> {
    return this.http.get<Product>(`${this.apiUrl}/${id}`);
  }

  create(data: CreateProductRequest): Observable<string> {
    return this.http.post<string>(this.apiUrl, data);
  }

  update(id: string, data: UpdateProductRequest): Observable<void> {
    return this.http.put<void>(`${this.apiUrl}/${id}`, data);
  }

  delete(id: string): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${id}`);
  }
}
