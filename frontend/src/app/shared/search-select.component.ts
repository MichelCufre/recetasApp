import { CommonModule } from '@angular/common';
import { Component, ElementRef, EventEmitter, forwardRef, Input, Output, ViewChild } from '@angular/core';
import { ControlValueAccessor, FormsModule, NG_VALUE_ACCESSOR } from '@angular/forms';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatSelectChange, MatSelectModule } from '@angular/material/select';

@Component({
  selector: 'app-search-select',
  standalone: true,
  imports: [CommonModule, FormsModule, MatFormFieldModule, MatSelectModule],
  providers: [{ provide: NG_VALUE_ACCESSOR, useExisting: forwardRef(() => SearchSelectComponent), multi: true }],
  styles: [`
    :host, mat-form-field { display:block; width:100%; }
    .search-box { position:sticky; top:0; z-index:2; padding:10px 12px; background:white; border-bottom:1px solid #e6ebe8; }
    .search-box input { width:100%; border:1px solid #cfd8d3; border-radius:7px; padding:9px 10px; outline:none; font:inherit; }
    .search-box input:focus { border-color:#246b50; box-shadow:0 0 0 2px #246b5020; }
    .empty { padding:12px 16px; color:#718078; font-size:12px; }
  `],
  template: `
    <mat-form-field appearance="outline">
      <mat-label>{{label}}</mat-label>
      <mat-select [value]="value" [multiple]="multiple" [disabled]="disabled"
        (selectionChange)="changed($event)" (openedChange)="opened($event)" (blur)="onTouched()">
        <div class="search-box" (click)="$event.stopPropagation()">
          <input #searchInput type="text" [(ngModel)]="query" [ngModelOptions]="{standalone:true}"
            [placeholder]="placeholder" (keydown)="$event.stopPropagation()" (click)="$event.stopPropagation()">
        </div>
        <mat-option *ngFor="let option of filteredOptions" [value]="optionValue(option)">{{optionLabel(option)}}</mat-option>
        <div class="empty" *ngIf="!filteredOptions.length">No hay coincidencias</div>
      </mat-select>
    </mat-form-field>
  `
})
export class SearchSelectComponent implements ControlValueAccessor {
  @Input() label = '';
  @Input() placeholder = 'Buscar...';
  @Input() options: any[] = [];
  @Input() valueKey = '';
  @Input() labelKey = '';
  @Input() multiple = false;
  @Input() value: any;
  @Output() valueChange = new EventEmitter<any>();
  @ViewChild('searchInput') searchInput?: ElementRef<HTMLInputElement>;
  query = '';
  disabled = false;
  private onChange: (value: any) => void = () => {};
  onTouched: () => void = () => {};

  get filteredOptions() {
    const term = this.normalize(this.query);
    return term ? this.options.filter(option => this.normalize(this.optionLabel(option)).includes(term)) : this.options;
  }
  optionValue(option: any) { return this.valueKey ? option?.[this.valueKey] : option; }
  optionLabel(option: any) { return String(this.labelKey ? option?.[this.labelKey] ?? '' : option ?? ''); }
  changed(event: MatSelectChange) { this.value = event.value; this.onChange(this.value); this.valueChange.emit(this.value); }
  opened(isOpen: boolean) { this.query = ''; if (isOpen) setTimeout(() => this.searchInput?.nativeElement.focus()); }
  writeValue(value: any) { this.value = value; }
  registerOnChange(fn: (value: any) => void) { this.onChange = fn; }
  registerOnTouched(fn: () => void) { this.onTouched = fn; }
  setDisabledState(disabled: boolean) { this.disabled = disabled; }
  private normalize(value: string) { return value.normalize('NFD').replace(/[\u0300-\u036f]/g, '').toLocaleLowerCase().trim(); }
}
