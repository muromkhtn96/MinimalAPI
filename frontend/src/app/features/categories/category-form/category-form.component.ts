import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';
import { Router, ActivatedRoute } from '@angular/router';
import { CategoryService } from '../../../core/services/category.service';

@Component({
  selector: 'app-category-form',
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './category-form.component.html',
  styleUrl: './category-form.component.scss'
})
export class CategoryFormComponent implements OnInit {
  private fb = inject(FormBuilder);
  private categoryService = inject(CategoryService);
  private router = inject(Router);
  private route = inject(ActivatedRoute);

  categoryForm: FormGroup;
  isEditMode = false;
  categoryId: string | null = null;
  loading = false;
  submitting = false;

  constructor() {
    this.categoryForm = this.fb.group({
      name: ['', [Validators.required, Validators.maxLength(100)]],
      description: ['']
    });
  }

  ngOnInit(): void {
    this.categoryId = this.route.snapshot.paramMap.get('id');
    if (this.categoryId) {
      this.isEditMode = true;
      this.loadCategory(this.categoryId);
    }
  }

  loadCategory(id: string): void {
    this.loading = true;
    this.categoryService.getById(id).subscribe({
      next: (category) => {
        this.categoryForm.patchValue({
          name: category.name,
          description: category.description
        });
        this.loading = false;
      },
      error: () => {
        this.loading = false;
        this.router.navigate(['/categories']);
      }
    });
  }

  onSubmit(): void {
    if (this.categoryForm.invalid) {
      this.categoryForm.markAllAsTouched();
      return;
    }

    this.submitting = true;
    const formValue = this.categoryForm.value;

    if (this.isEditMode && this.categoryId) {
      this.categoryService.update(this.categoryId, formValue).subscribe({
        next: () => {
          alert('Cập nhật danh mục thành công');
          this.router.navigate(['/categories']);
        },
        error: () => {
          this.submitting = false;
        }
      });
    } else {
      this.categoryService.create(formValue).subscribe({
        next: () => {
          alert('Tạo danh mục thành công');
          this.router.navigate(['/categories']);
        },
        error: () => {
          this.submitting = false;
        }
      });
    }
  }

  onCancel(): void {
    this.router.navigate(['/categories']);
  }

  get f() {
    return this.categoryForm.controls;
  }
}
