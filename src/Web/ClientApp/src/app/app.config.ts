import { APP_INITIALIZER, ApplicationConfig } from '@angular/core';
import { provideRouter } from '@angular/router';
import { provideHttpClient, withFetch, withInterceptors } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';

import { routes } from './app.routes';
import { authInterceptor } from './core/services/auth.interceptor';
import { AuthService } from './core/services/auth.service';
import { SignalRService } from './core/signalr/signalr.service';

export const appConfig: ApplicationConfig = {
  providers: [
    provideRouter(routes),
    provideHttpClient(withFetch(), withInterceptors([authInterceptor])),
    {
      provide: APP_INITIALIZER,
      useFactory: (authService: AuthService, signalRService: SignalRService) => () =>
        firstValueFrom(authService.initializeAuth()).then(restored => {
          if (restored) {
            signalRService.start();
          }
          return restored;
        }),
      deps: [AuthService, SignalRService],
      multi: true,
    },
  ],
};
