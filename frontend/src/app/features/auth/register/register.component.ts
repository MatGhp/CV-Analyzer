import { Component, inject, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators, AbstractControl, ValidationErrors } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';
import { RegisterRequest } from '../../../core/models/auth.model';

@Component({
  selector: 'app-register',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  templateUrl: './register.component.html',
  styleUrl: './register.component.scss'
})
export class RegisterComponent {
  private readonly fb = inject(FormBuilder);
  private readonly authService = inject(AuthService);
  private readonly router = inject(Router);

  isLoading = signal(false);
  errorMessage = signal<string | null>(null);
  hasGuestData = computed(() => !!this.authService.getGuestSessionToken());

  registerForm = this.fb.group({
    fullName: ['', [Validators.required, Validators.maxLength(200)]],
    email: ['', [Validators.required, Validators.email, Validators.maxLength(200)]],
    phone: ['', [Validators.maxLength(50)]],
    password: ['', [Validators.required, Validators.minLength(8), this.passwordValidator]],
    confirmPassword: ['', [Validators.required]]
  }, { validators: this.passwordMatchValidator });

  onSubmit(): void {
    if (this.registerForm.invalid) {
      this.registerForm.markAllAsTouched();
      return;
    }

    this.isLoading.set(true);
    this.errorMessage.set(null);

    const request: RegisterRequest = {
      fullName: this.registerForm.value.fullName!,
      email: this.registerForm.value.email!,
      password: this.registerForm.value.password!,
      phone: this.registerForm.value.phone || undefined,
      guestSessionToken: this.authService.getGuestSessionToken() || undefined
    };

    this.authService.register(request).subscribe({
      next: (response) => {
        console.log('Registration successful:', response);
        this.isLoading.set(false);
        
        // Clear guest session token if migration occurred
        if (response.migratedGuestData) {
          this.authService.clearGuestSessionToken();
          console.log(`Migrated ${response.migratedResumeCount} guest resume(s)`);
        }
        
        this.router.navigate(['/dashboard']);
      },
      error: (error) => {
        console.error('Registration error:', error);
        this.isLoading.set(false);
        
        if (error.status === 400 && error.error?.errors) {
          // Validation errors from backend
          const errors = error.error.errors;
          const errorMessages = errors.map((e: any) => e.errorMessage).join(', ');
          this.errorMessage.set(errorMessages);
        } else if (error.error?.message) {
          this.errorMessage.set(error.error.message);
        } else {
          this.errorMessage.set('An error occurred during registration. Please try again.');
        }
      }
    });
  }

  /**
   * Custom validator for password complexity
   */
  private passwordValidator(control: AbstractControl): ValidationErrors | null {
    const password = control.value;
    
    if (!password) {
      return null; // 'required' validator handles this
    }

    const errors: ValidationErrors = {};
    
    if (!/[A-Z]/.test(password)) {
      errors['uppercase'] = true;
    }
    
    if (!/[a-z]/.test(password)) {
      errors['lowercase'] = true;
    }
    
    if (!/\d/.test(password)) {
      errors['digit'] = true;
    }
    
    if (!/[@$!%*?&]/.test(password)) {
      errors['specialChar'] = true;
    }
    
    return Object.keys(errors).length > 0 ? errors : null;
  }

  /**
   * Custom validator for password match
   */
  private passwordMatchValidator(control: AbstractControl): ValidationErrors | null {
    const password = control.get('password');
    const confirmPassword = control.get('confirmPassword');
    
    if (!password || !confirmPassword) {
      return null;
    }
    
    return password.value === confirmPassword.value ? null : { passwordMismatch: true };
  }

  getFieldError(fieldName: string): string | null {
    const field = this.registerForm.get(fieldName);
    
    if (!field?.touched) {
      return null;
    }
    
    if (field.hasError('required')) {
      return `${this.getFieldLabel(fieldName)} is required`;
    }
    
    if (field.hasError('email')) {
      return 'Please enter a valid email address';
    }
    
    if (field.hasError('minlength')) {
      return `${this.getFieldLabel(fieldName)} must be at least ${field.errors?.['minlength'].requiredLength} characters`;
    }
    
    if (field.hasError('maxlength')) {
      return `${this.getFieldLabel(fieldName)} must not exceed ${field.errors?.['maxlength'].requiredLength} characters`;
    }
    
    if (fieldName === 'password') {
      if (field.hasError('uppercase')) {
        return 'Password must contain at least one uppercase letter';
      }
      if (field.hasError('lowercase')) {
        return 'Password must contain at least one lowercase letter';
      }
      if (field.hasError('digit')) {
        return 'Password must contain at least one digit';
      }
      if (field.hasError('specialChar')) {
        return 'Password must contain at least one special character (@$!%*?&)';
      }
    }
    
    if (fieldName === 'confirmPassword' && this.registerForm.hasError('passwordMismatch')) {
      return 'Passwords do not match';
    }
    
    return null;
  }

  private getFieldLabel(fieldName: string): string {
    const labels: { [key: string]: string } = {
      fullName: 'Full name',
      email: 'Email',
      phone: 'Phone',
      password: 'Password',
      confirmPassword: 'Confirm password'
    };
    return labels[fieldName] || fieldName;
  }
}
