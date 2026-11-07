import { HttpErrorResponse } from '@angular/common/http';
import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { AuthService } from '../auth/auth.service';

@Component({
  selector: 'app-login',
  imports: [ReactiveFormsModule, RouterLink],
  templateUrl: './login.html',
  styleUrl: './login.css',
})
export class Login {
  private readonly formBuilder = inject(FormBuilder);
  readonly maxDob = new Date().toISOString().slice(0, 10);
  readonly isSignUp = signal(false);
  readonly isSubmitting = signal(false);
  readonly errorMessage = signal<string | null>(null);
  readonly form = this.formBuilder.nonNullable.group({
    name: [''],
    email: ['', [Validators.required, Validators.email, Validators.maxLength(254)]],
    mobile: [''],
    gender: [''],
    dob: [''],
    password: ['', [Validators.required, Validators.maxLength(128)]],
  });

  constructor(
    private readonly auth: AuthService,
    private readonly router: Router,
    private readonly route: ActivatedRoute,
  ) {}

  setSignUp(enabled: boolean): void {
    this.isSignUp.set(enabled);
    this.errorMessage.set(null);

    const profileValidators = enabled ? [Validators.required] : [];
    this.form.controls.name.setValidators(
      enabled ? [Validators.required, Validators.maxLength(80)] : profileValidators,
    );
    this.form.controls.mobile.setValidators(
      enabled
        ? [Validators.required, Validators.pattern(/^(?=(?:.*\d){7,})[0-9+()\-\s]{7,20}$/)]
        : profileValidators,
    );
    this.form.controls.gender.setValidators(
      enabled ? [Validators.required, Validators.maxLength(40)] : profileValidators,
    );
    this.form.controls.dob.setValidators(enabled ? Validators.required : profileValidators);
    this.form.controls.password.setValidators(
      enabled
        ? [Validators.required, Validators.minLength(12), Validators.maxLength(128)]
        : [Validators.required, Validators.maxLength(128)],
    );

    for (const control of Object.values(this.form.controls)) {
      control.updateValueAndValidity();
    }
  }

  submit(): void {
    this.errorMessage.set(null);
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.isSubmitting.set(true);
    const values = this.form.getRawValue();
    const request = this.isSignUp()
      ? this.auth.register({
          name: values.name,
          email: values.email,
          mobile: values.mobile,
          gender: values.gender,
          dob: values.dob,
          password: values.password,
        })
      : this.auth.login({ email: values.email, password: values.password });

    request.subscribe({
      next: () => {
        const returnUrl = this.route.snapshot.queryParamMap.get('returnUrl');
        const destination = returnUrl?.startsWith('/') && !returnUrl.startsWith('//')
          ? returnUrl
          : '/users';
        void this.router.navigateByUrl(destination);
      },
      error: (error: unknown) => {
        this.isSubmitting.set(false);
        this.errorMessage.set(
          error instanceof HttpErrorResponse && error.status === 401
            ? 'The email or password is incorrect.'
            : error instanceof HttpErrorResponse && error.status === 409
              ? 'An account with this email already exists. Sign in instead.'
              : this.isSignUp()
                ? 'Sign up failed. Please check your details and try again.'
                : 'Sign in failed. Please try again.',
        );
      },
      complete: () => this.isSubmitting.set(false),
    });
  }
}
