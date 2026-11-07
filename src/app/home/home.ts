import { Component, OnInit, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { UserService } from '../services/user.service';

@Component({
  selector: 'app-home',
  imports: [RouterLink],
  templateUrl: './home.html',
  styleUrl: './home.css',
})
export class Home implements OnInit {
  readonly userCount = signal<number | null>(null);
  readonly errorMessage = signal('');

  constructor(private readonly userService: UserService) {}

  ngOnInit(): void {
    this.userService.getUsers().subscribe({
      next: (users) => {
        this.userCount.set(users.length);
      },
      error: () => {
        this.errorMessage.set('Could not load directory details. Check that the API is running.');
      },
    });
  }
}
