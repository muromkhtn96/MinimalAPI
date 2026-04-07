export interface Product {
  id: string;
  name: string;
  price: number;
  currency: string;
  categoryId: string;
  categoryName: string;
  description?: string;
  isActive: boolean;
  createdAt: Date;
}

export interface CreateProductRequest {
  name: string;
  price: number;
  currency: string;
  categoryId: string;
  description?: string;
}

export interface UpdateProductRequest {
  name: string;
  price: number;
  currency: string;
  categoryId: string;
  description?: string;
  isActive: boolean;
}
