import { Routes } from '@angular/router';
import { HomeComponent } from './features/home/home.component';
import { ProductListComponent } from './features/products/product-list/product-list.component';
import { ProductFormComponent } from './features/products/product-form/product-form.component';
import { CategoryListComponent } from './features/categories/category-list/category-list.component';
import { CategoryFormComponent } from './features/categories/category-form/category-form.component';

export const routes: Routes = [
  { path: '', component: HomeComponent },
  { path: 'products', component: ProductListComponent },
  { path: 'products/create', component: ProductFormComponent },
  { path: 'products/edit/:id', component: ProductFormComponent },
  { path: 'categories', component: CategoryListComponent },
  { path: 'categories/create', component: CategoryFormComponent },
  { path: 'categories/edit/:id', component: CategoryFormComponent },
  { path: '**', redirectTo: '' }
];
