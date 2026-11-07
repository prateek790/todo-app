import { Routes } from '@angular/router';
import { Home } from './home/home';
import { UserForm } from './user-form/user-form';
import { UserList } from './user-list/user-list';
import { Login } from './login/login';
import { authGuard, guestGuard } from './auth/auth.guard';

export const routes: Routes = [
  { path: 'login', component: Login, canActivate: [guestGuard], title: 'Sign in | PeopleDesk' },
  { path: '', component: Home, canActivate: [authGuard], title: 'Home | PeopleDesk' },
  { path: 'users', component: UserList, canActivate: [authGuard], title: 'Users | PeopleDesk' },
  { path: 'users/new', component: UserForm, canActivate: [authGuard], title: 'Add user | PeopleDesk' },
  { path: 'users/:id/edit', component: UserForm, canActivate: [authGuard], title: 'Edit user | PeopleDesk' },
  { path: '**', redirectTo: '' },
];
