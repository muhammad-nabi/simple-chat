import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import {
  FormBuilder,
  FormGroup,
  ReactiveFormsModule,
  Validators,
} from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  templateUrl: './login.component.html',
  styleUrl: './login.component.scss',
})
export class LoginComponent {
  private fb = inject(FormBuilder);
  private authService = inject(AuthService);
  private router = inject(Router);

  form: FormGroup = this.fb.group({
    email: ['', [Validators.required, Validators.email]],
    password: ['', [Validators.required]],
  }, { updateOn: 'blur' });

  serverError = '';
  isSubmitting = false;

  get email() { return this.form.get('email')!; }
  get password() { return this.form.get('password')!; }

  getErrorMessage(fieldName: string): string {
    const control = this.form.get(fieldName);
    if (!control?.touched || !control.errors) return '';

    switch (fieldName) {
      case 'email':
        if (control.errors['required']) return 'Email is required.';
        if (control.errors['email']) return 'Please enter a valid email.';
        break;
      case 'password':
        if (control.errors['required']) return 'Password is required.';
        break;
    }
    return '';
  }

  onSubmit(): void {
    if (this.form.invalid || this.isSubmitting) return;

    this.serverError = '';
    this.isSubmitting = true;

    this.authService.login(this.form.value).subscribe({
      next: () => {
        this.isSubmitting = false;
        this.router.navigate(['/']);
      },
      error: (err) => {
        this.isSubmitting = false;

        // Clear password on error, preserve email
        this.form.patchValue({ password: '' });
        this.password.markAsUntouched();

        if (err.status === 429) {
          this.serverError = 'Too many login attempts. Please try again later.';
        } else if (err.status === 401) {
          this.serverError = err.error?.detail || 'Invalid email or password.';
        } else {
          this.serverError = 'An unexpected error occurred. Please try again.';
        }
      },
    });
  }
}
