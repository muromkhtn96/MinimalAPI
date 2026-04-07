import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { CategoryService } from '../../../core/services/category.service';
import { Category } from '../../../core/models/category.model';
import { PagedResult } from '../../../core/models/paged-result.model';

@Component({
  selector: 'app-category-list',
  imports: [CommonModule, RouterLink],
  templateUrl: './category-list.component.html',
  styleUrl: './category-list.component.scss'
})
export class CategoryListComponent implements OnInit {
  private categoryService = inject(CategoryService);

  categories: Category[] = [];
  pagedResult: PagedResult<Category> | null = null;
  currentPage = 1;
  pageSize = 20;
  loading = false;

  ngOnInit(): void {
    this.loadCategories();
  }

  loadCategories(): void {
    this.loading = true;
    this.categoryService.getAll(this.currentPage, this.pageSize)
      .subscribe({
        next: (result) => {
          this.pagedResult = result;
          this.categories = result.items;
          this.loading = false;
        },
        error: () => {
          this.loading = false;
        }
      });
  }

  onPageChange(page: number): void {
    this.currentPage = page;
    this.loadCategories();
  }

  onDelete(id: string, name: string): void {
    if (confirm(`Bạn có chắc muốn xóa danh mục "${name}"?`)) {
      this.loading = true;
      this.categoryService.delete(id).subscribe({
        next: () => {
          alert('Xóa danh mục thành công');
          this.loadCategories();
        },
        error: () => {
          this.loading = false;
        }
      });
    }
  }
}
