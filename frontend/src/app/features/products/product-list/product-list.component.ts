import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { ProductService } from '../../../core/services/product.service';
import { Product } from '../../../core/models/product.model';
import { PagedResult } from '../../../core/models/paged-result.model';

@Component({
  selector: 'app-product-list',
  imports: [CommonModule, RouterLink, FormsModule],
  templateUrl: './product-list.component.html',
  styleUrl: './product-list.component.scss'
})
export class ProductListComponent implements OnInit {
  private productService = inject(ProductService);

  products: Product[] = [];
  pagedResult: PagedResult<Product> | null = null;
  currentPage = 1;
  pageSize = 10;
  searchTerm = '';
  loading = false;

  ngOnInit(): void {
    this.loadProducts();
  }

  loadProducts(): void {
    this.loading = true;
    this.productService.getAll(this.currentPage, this.pageSize, this.searchTerm || undefined)
      .subscribe({
        next: (result) => {
          this.pagedResult = result;
          this.products = result.items;
          this.loading = false;
        },
        error: () => {
          this.loading = false;
        }
      });
  }

  onSearch(): void {
    this.currentPage = 1;
    this.loadProducts();
  }

  onPageChange(page: number): void {
    this.currentPage = page;
    this.loadProducts();
  }

  onDelete(id: string, name: string): void {
    if (confirm(`Bạn có chắc muốn xóa sản phẩm "${name}"?`)) {
      this.loading = true;
      this.productService.delete(id).subscribe({
        next: () => {
          alert('Xóa sản phẩm thành công');
          this.loadProducts();
        },
        error: () => {
          this.loading = false;
        }
      });
    }
  }
}
