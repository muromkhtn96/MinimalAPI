import { HttpInterceptorFn, HttpErrorResponse } from '@angular/common/http';
import { catchError, throwError } from 'rxjs';

export const errorInterceptor: HttpInterceptorFn = (req, next) => {
  return next(req).pipe(
    catchError((error: HttpErrorResponse) => {
      let errorMessage = '';

      if (error.error instanceof ErrorEvent) {
        // Client-side error
        errorMessage = `Lỗi: ${error.error.message}`;
      } else {
        // Server-side error
        switch (error.status) {
          case 400:
            errorMessage = 'Dữ liệu không hợp lệ';
            if (error.error?.errors) {
              const validationErrors = Object.values(error.error.errors).flat();
              errorMessage = validationErrors.join('\n');
            } else if (error.error?.error) {
              errorMessage = error.error.error;
            }
            break;
          case 404:
            errorMessage = 'Không tìm thấy dữ liệu';
            break;
          case 500:
            errorMessage = 'Lỗi hệ thống. Vui lòng thử lại sau';
            break;
          default:
            errorMessage = `Lỗi: ${error.status} - ${error.message}`;
        }
      }

      console.error('HTTP Error:', errorMessage);
      alert(errorMessage);
      return throwError(() => new Error(errorMessage));
    })
  );
};
