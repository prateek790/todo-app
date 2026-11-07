import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideRouter } from '@angular/router';

import { Login } from './login';

describe('Login', () => {
  let component: Login;
  let fixture: ComponentFixture<Login>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [Login],
      providers: [provideHttpClient(), provideRouter([])],
    })
    .compileComponents();

    fixture = TestBed.createComponent(Login);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('requires profile details and a 12-character password when signing up', () => {
    component.setSignUp(true);
    expect(component.form.invalid).toBe(true);

    component.form.setValue({
      name: 'Jordan Lee',
      email: 'jordan@example.com',
      mobile: '5551234567',
      gender: 'Non-binary',
      dob: '1995-04-12',
      password: 'a-strong-password',
    });
    expect(component.form.valid).toBe(true);

    component.form.controls.password.setValue('short');
    expect(component.form.invalid).toBe(true);
  });
});
