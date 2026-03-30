import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import {
  FormBuilder,
  FormGroup,
  ReactiveFormsModule,
  Validators,
} from '@angular/forms';
import { Router } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';

@Component({
  selector: 'app-register',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './register.component.html',
  styleUrl: './register.component.scss',
})
export class RegisterComponent {
  private fb = inject(FormBuilder);
  private authService = inject(AuthService);
  private router = inject(Router);

  form: FormGroup = this.fb.group({
    displayName: ['', [Validators.required, Validators.maxLength(256)]],
    email: ['', [Validators.required, Validators.email, Validators.maxLength(254)]],
    password: ['', [Validators.required, Validators.minLength(8)]],
  }, { updateOn: 'blur' });

  serverError = '';
  isSubmitting = false;

  get displayName() { return this.form.get('displayName')!; }
  get email() { return this.form.get('email')!; }
  get password() { return this.form.get('password')!; }

  getErrorMessage(fieldName: string): string {
    const control = this.form.get(fieldName);
    if (!control?.touched || !control.errors) return '';

    switch (fieldName) {
      case 'displayName':
        if (control.errors['required']) return 'Display name is required.';
        if (control.errors['maxlength']) return 'Display name must not exceed 256 characters.';
        break;
      case 'email':
        if (control.errors['required']) return 'Email is required.';
        if (control.errors['email']) return 'Please enter a valid email.';
        if (control.errors['maxlength']) return 'Email must not exceed 254 characters.';
        break;
      case 'password':
        if (control.errors['required']) return 'Password is required.';
        if (control.errors['minlength']) return 'Password must be at least 8 characters.';
        break;
    }
    return '';
  }

  onSubmit(): void {
    if (this.form.invalid || this.isSubmitting) return;

    this.serverError = '';
    this.isSubmitting = true;

    this.authService.register(this.form.value).subscribe({
      next: () => {
        this.router.navigate(['/']);
      },
      error: (err) => {
        this.isSubmitting = false;
        if (err.status === 400 && err.error?.errors) {
          const errors = err.error.errors;
          // Map server validation errors to specific field or general message
          const emailErrors = errors['Email'] || errors['email'];
          if (emailErrors?.length) {
            this.serverError = emailErrors[0];
          } else {
            // Combine all error messages
            const allErrors = Object.values(errors).flat();
            this.serverError = (allErrors as string[])[0] || 'Registration failed. Please try again.';
          }
        } else {
          this.serverError = 'An unexpected error occurred. Please try again.';
        }
      },
    });
  }
}
