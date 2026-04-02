import { Component, Input } from '@angular/core';
import { CheckmarkState } from '../../models/message.model';

@Component({
  selector: 'app-checkmark',
  standalone: true,
  imports: [],
  templateUrl: './checkmark.component.html',
  styleUrl: './checkmark.component.scss',
})
export class CheckmarkComponent {
  @Input({ required: true }) state!: CheckmarkState;
  @Input() animate = false;
}
