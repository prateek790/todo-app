import { DatePipe } from '@angular/common';
import { Component, computed, OnInit, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { UserService } from '../services/user.service';
import { User } from '../user';

@Component({
  selector: 'app-user-list',
  imports: [DatePipe, RouterLink],
  templateUrl: './user-list.html',
  styleUrl: './user-list.css',
})
export class UserList implements OnInit {
  readonly searchTerm = signal('');
  readonly users = signal<User[]>([]);
  readonly isLoading = signal(true);
  readonly errorMessage = signal('');
  readonly filteredUsers = computed(() => {
    const users = this.users();
    const query = this.searchTerm().trim().toLowerCase();
    if (!query) {
      return users;
    }

    return users.filter((user) =>
      [user.name, user.email, user.mobile, user.userType ?? '']
        .join(' ')
        .toLowerCase()
        .includes(query),
    );
  });

  constructor(private readonly userService: UserService) {}

  ngOnInit(): void {
    this.loadUsers();
  }

  loadUsers(): void {
    this.isLoading.set(true);
    this.errorMessage.set('');
    this.userService.getUsers().subscribe({
      next: (users) => {
        this.users.set(users);
        this.isLoading.set(false);
      },
      error: () => {
        this.errorMessage.set('Could not load users. Check that the API and SQL Server are running.');
        this.isLoading.set(false);
      },
    });
  }

  onSearch(event: Event): void {
    if (event.target instanceof HTMLInputElement) {
      this.searchTerm.set(event.target.value);
    }
  }

  deleteUser(user: User): void {
    if (window.confirm(`Delete ${user.name} from the directory?`)) {
      this.errorMessage.set('');
      this.userService.removeUser(user.id).subscribe({
        next: () => {
          this.users.update((users) => users.filter((item) => item.id !== user.id));
        },
        error: () => {
          this.errorMessage.set(`Could not delete ${user.name}. Please try again.`);
        },
      });
    }
  }
}
