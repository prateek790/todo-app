import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { map, Observable } from 'rxjs';
import { User } from '../user';

@Injectable({ providedIn: 'root' })
export class UserService {
  private readonly usersUrl = '/api/users';

  constructor(private readonly http: HttpClient) {}

  getUsers(): Observable<User[]> {
    return this.http
      .get<User[]>(this.usersUrl)
      .pipe(map((users) => users.map((user) => this.normalizeUser(user))));
  }

  getUserById(id: number): Observable<User> {
    return this.http
      .get<User>(`${this.usersUrl}/${id}`)
      .pipe(map((user) => this.normalizeUser(user)));
  }

  addUser(user: Omit<User, 'id'>): Observable<User> {
    return this.http
      .post<User>(this.usersUrl, user)
      .pipe(map((createdUser) => this.normalizeUser(createdUser)));
  }

  updateUser(user: User): Observable<User> {
    return this.http
      .put<User>(`${this.usersUrl}/${user.id}`, user)
      .pipe(map((updatedUser) => this.normalizeUser(updatedUser)));
  }

  removeUser(id: number): Observable<void> {
    return this.http.delete<void>(`${this.usersUrl}/${id}`);
  }

  private normalizeUser(user: User): User {
    return { ...user, dob: new Date(user.dob) };
  }
}
