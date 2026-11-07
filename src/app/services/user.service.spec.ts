import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { User } from '../user';
import { UserService } from './user.service';

describe('UserService', () => {
  let service: UserService;
  let httpTestingController: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting(), UserService],
    });
    service = TestBed.inject(UserService);
    httpTestingController = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpTestingController.verify();
  });

  it('uses the API for user CRUD and converts response dates', () => {
    const user: User = {
      id: 2,
      name: 'Taylor Reed',
      email: 'taylor@example.com',
      mobile: '5551234567',
      gender: 'Non-binary',
      dob: new Date('1995-04-12T00:00:00.000Z'),
      isActive: true,
      range: [2, 8],
      userType: 'User',
    };

    service.getUsers().subscribe((users) => {
      expect(users[0].dob).toBeInstanceOf(Date);
      expect(users[0].name).toBe(user.name);
    });
    const listRequest = httpTestingController.expectOne('/api/users');
    expect(listRequest.request.method).toBe('GET');
    listRequest.flush([user]);

    const newUser: Omit<User, 'id'> = {
      name: user.name,
      email: user.email,
      mobile: user.mobile,
      gender: user.gender,
      dob: user.dob,
      isActive: user.isActive,
      range: user.range,
      userType: user.userType,
    };
    service.addUser(newUser).subscribe((created) => {
      expect(created.id).toBe(user.id);
    });
    const createRequest = httpTestingController.expectOne('/api/users');
    expect(createRequest.request.method).toBe('POST');
    createRequest.flush(user);

    const updatedUser = { ...user, name: 'Taylor Morgan' };
    service.updateUser(updatedUser).subscribe((updated) => {
      expect(updated.name).toBe('Taylor Morgan');
    });
    const updateRequest = httpTestingController.expectOne('/api/users/2');
    expect(updateRequest.request.method).toBe('PUT');
    updateRequest.flush(updatedUser);

    service.removeUser(user.id).subscribe();
    const deleteRequest = httpTestingController.expectOne('/api/users/2');
    expect(deleteRequest.request.method).toBe('DELETE');
    deleteRequest.flush(null);
  });
});
