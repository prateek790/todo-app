import { Component, inject, OnInit, signal } from '@angular/core';
import {
  AbstractControl,
  FormBuilder,
  ReactiveFormsModule,
  ValidationErrors,
  ValidatorFn,
  Validators,
} from '@angular/forms';
import { HttpErrorResponse } from '@angular/common/http';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { UserService } from '../services/user.service';
import { User } from '../user';

const rangeOrderValidator: ValidatorFn = (control: AbstractControl): ValidationErrors | null => {
  const start = control.get('rangeStart')?.value;
  const end = control.get('rangeEnd')?.value;
  return typeof start === 'number' && typeof end === 'number' && start > end
    ? { rangeOrder: true }
    : null;
};

@Component({
  selector: 'app-user-form',
  imports: [ReactiveFormsModule, RouterLink],
  templateUrl: './user-form.html',
  styleUrl: './user-form.css',
})
export class UserForm implements OnInit {
  private readonly formBuilder = inject(FormBuilder);

  readonly form = this.formBuilder.nonNullable.group({
    name: ['', [Validators.required, Validators.pattern(/\S/), Validators.maxLength(80)]],
    email: ['', [Validators.required, Validators.email]],
    mobile: ['', [Validators.required, Validators.pattern(/^(?=(?:.*\d){7,})[0-9+()\-\s]{7,20}$/)]],
    gender: ['', Validators.required],
    dob: ['', Validators.required],
    isActive: true,
    userType: ['User', Validators.required],
    rangeStart: [0, [Validators.min(0), Validators.max(10)]],
    rangeEnd: [10, [Validators.min(0), Validators.max(10)]],
  }, { validators: rangeOrderValidator });

  readonly userId = signal<number | null>(null);
  readonly notFound = signal(false);
  submitted = false;
  readonly isLoading = signal(false);
  readonly isSaving = signal(false);
  readonly errorMessage = signal('');
  private originalUser: User | null = null;

  constructor(
    private readonly route: ActivatedRoute,
    private readonly router: Router,
    private readonly userService: UserService,
  ) {}

  get isEditing(): boolean {
    return this.userId() !== null;
  }

  ngOnInit(): void {
    const idParam = this.route.snapshot.paramMap.get('id');
    if (idParam === null) {
      return;
    }

    const id = Number(idParam);
    if (!Number.isSafeInteger(id) || id < 1) {
      this.notFound.set(true);
      return;
    }

    this.userId.set(id);
    this.isLoading.set(true);
    this.userService.getUserById(id).subscribe({
      next: (user) => {
        this.userId.set(user.id);
        this.originalUser = user;
        this.setFormFromUser(user);
        this.isLoading.set(false);
      },
      error: (error: unknown) => {
        this.isLoading.set(false);
        if (error instanceof HttpErrorResponse && error.status === 404) {
          this.notFound.set(true);
        } else {
          this.errorMessage.set('Could not load this user. Check that the API and SQL Server are running.');
        }
      },
    });
  }

  save(): void {
    this.submitted = true;
    if (this.form.invalid || this.notFound() || this.isLoading() || this.isSaving()) {
      this.form.markAllAsTouched();
      return;
    }

    const values = this.form.getRawValue();
    const userData = {
      name: values.name.trim(),
      email: values.email.trim(),
      mobile: values.mobile.trim(),
      gender: values.gender,
      dob: new Date(`${values.dob}T00:00:00`),
      isActive: values.isActive,
      userType: values.userType,
      range: [values.rangeStart, values.rangeEnd],
    };

    this.errorMessage.set('');
    this.isSaving.set(true);
    const userId = this.userId();
    const request = userId === null
      ? this.userService.addUser(userData)
      : this.userService.updateUser({ ...userData, id: userId });
    request.subscribe({
      next: () => {
        void this.router.navigate(['/users']);
      },
      error: (error: unknown) => {
        this.isSaving.set(false);
        if (error instanceof HttpErrorResponse && error.status === 404 && userId !== null) {
          this.notFound.set(true);
        } else if (error instanceof HttpErrorResponse && error.status === 400) {
          this.errorMessage.set('Some details were rejected. Review the form and try again.');
        } else {
          this.errorMessage.set('Could not save this user. Check that the API and SQL Server are running.');
        }
      },
    });
  }

  reset(): void {
    if (this.originalUser) {
      this.setFormFromUser(this.originalUser);
      this.submitted = false;
      this.errorMessage.set('');
      return;
    }

    this.form.reset({
      name: '',
      email: '',
      mobile: '',
      gender: '',
      dob: '',
      isActive: true,
      userType: 'User',
      rangeStart: 0,
      rangeEnd: 10,
    });
    this.submitted = false;
    this.errorMessage.set('');
  }

  private setFormFromUser(user: User): void {
    this.form.reset({
      name: user.name,
      email: user.email,
      mobile: user.mobile,
      gender: user.gender,
      dob: this.toDateInputValue(user.dob),
      isActive: user.isActive,
      userType: user.userType || 'User',
      rangeStart: user.range?.[0] ?? 0,
      rangeEnd: user.range?.[1] ?? 10,
    });
  }

  private toDateInputValue(date: Date): string {
    const year = date.getFullYear();
    const month = String(date.getMonth() + 1).padStart(2, '0');
    const day = String(date.getDate()).padStart(2, '0');
    return `${year}-${month}-${day}`;
  }
}
