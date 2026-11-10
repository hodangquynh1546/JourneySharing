import type { CapacitorConfig } from '@capacitor/cli';

const config: CapacitorConfig = {
  appId: 'com.journeysharing.mobile',
  appName: 'Journey Sharing',
  webDir: 'dist/journey-sharing-ui/browser',
  android: { allowMixedContent: false },
};

export default config;
