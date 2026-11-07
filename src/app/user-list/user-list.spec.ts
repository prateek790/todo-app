import { provideZonelessChangeDetection } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { Subject } from 'rxjs';
import { UserService } from '../services/user.service';
import { User } from '../user';
import { UserList } from './user-list';

describe('UserList', () => {
  it('updates the directory when the API response arrives in zoneless mode', async () => {
    const usersResponse = new Subject<User[]>();
    await TestBed.configureTestingModule({
      imports: [UserList],
      providers: [
        provideRouter([]),
        provideZonelessChangeDetection(),
        {
          provide: UserService,
          useValue: { getUsers: () => usersResponse.asObservable() },
        },
      ],
    }).compileComponents();

    const fixture = TestBed.createComponent(UserList);
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('Loading users...');

    usersResponse.next([
      {
        id: 1,
        name: 'Ankit Sahu',
        email: 'ankit@gmail.com',
        mobile: '8978786933',
        gender: 'Male',
        dob: new Date(1992, 7, 31),
        isActive: true,
        userType: 'Admin',
      },
    ]);

    await fixture.whenStable();
    expect(fixture.nativeElement.querySelector('.person-name')?.textContent).toContain('Ankit Sahu');
    expect(fixture.nativeElement.textContent).not.toContain('Loading users...');
  });
});
